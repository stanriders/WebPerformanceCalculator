
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RunProcessAsTask;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.Models;
using WebPerformanceCalculator.Shared;

namespace WebPerformanceCalculator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private static readonly ConcurrentQueue<string> usernameQueue = new ConcurrentQueue<string>();
        private static DateTime queueDebounce = DateTime.Now;
        private static bool queueLocked;
        private static string workingDir;

        private static MemoryCache playerCache = new MemoryCache("calculated_players");

        private static readonly Regex mapLinkRegex = 
            new Regex(@"(?>https?:\/\/)?(?>osu|old)\.ppy\.sh\/([b,s]|(?>beatmaps)|(?>beatmapsets))\/(\d+\/?\#osu\/)?(\d+)?\/?(?>[&,?].=\d)?", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const double keep_scores_bigger_than = 699.5;

        private const string calc_file = "osu.Game.Rulesets.Osu.dll";
        private const string calc_update_link = "http://dandan.kikoe.ru/osu.Game.Rulesets.Osu.dll";

        public ApiController()
        {
            var assemblyFileInfo = new FileInfo(typeof(Program).Assembly.Location);
            workingDir = assemblyFileInfo.DirectoryName;
        }

        [Route("GetCalcModuleUpdateDate")]
        public IActionResult GetCalcModuleUpdateDate()
        {
            return Json(new
            {
                date = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime(), 
                commit = "d92b1d9"
            });
        }

        [HttpPost]
        [Route("AddToQueue")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult AddToQueue([FromForm] string jsonUsername)
        {
            if (string.IsNullOrEmpty(jsonUsername))
                return StatusCode(400, new { err = "Incorrect username" });

            if (queueLocked)
                return StatusCode(400, new { err = "Queue is temporary locked, try again later!" });

            if (queueDebounce > DateTime.Now)
                return StatusCode(400, new {err = "Try again later"});

            // performance calculator doesn't want to work with them even when escaping
            if (jsonUsername.Contains('-'))
                return StatusCode(400, new {err = "Please use user ID instead"});

            jsonUsername = jsonUsername.Trim();

            var regexp = new Regex(@"^[A-Za-z0-9-\[\]_ ]+$");

            if (jsonUsername.Length > 2 && jsonUsername.Length < 16 && regexp.IsMatch(jsonUsername))
            {
                jsonUsername = HttpUtility.HtmlEncode(jsonUsername).ToLowerInvariant();
                if (!usernameQueue.Contains(jsonUsername) && !playerCache.Contains(jsonUsername))
                {
                    usernameQueue.Enqueue(jsonUsername);
                    queueDebounce = DateTime.Now.AddSeconds(1);
                    return GetQueue();
                }

                return StatusCode(400, new { err = "This player doesn't need a recalculation yet! You can only recalculate once a day" });
            }

            return StatusCode(400, new {err = "Incorrect username"});
        }

        [Route("GetQueue")]
        public IActionResult GetQueue()
        {
            return Json(usernameQueue.ToArray());
        }

        [Route("GetResults")]
        public async Task<IActionResult> GetResults(string jsonUsername)
        {
            var result = string.Empty;
            if (System.IO.File.Exists($"players/{jsonUsername}.json"))
            {
                dynamic json = JsonConvert.DeserializeObject(await System.IO.File.ReadAllTextAsync($"players/{jsonUsername}.json"));
                json.UpdateDate = System.IO.File.GetLastWriteTime($"players/{jsonUsername}.json").ToUniversalTime();
                result = JsonConvert.SerializeObject(json);
            }

            return Json(result);
        }

        [Route("GetTop")]
        public async Task<IActionResult> GetTop(int offset = 0, int limit = 50, string search = null, string order = "asc", string sort = "localPP")
        {
            await using (DatabaseContext db = new DatabaseContext())
            {
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var totalNotFilteredAmt = await db.Players.CountAsync();
                var totalFilteredAmt = 0;

                var jsonPlayers = new List<TopPlayerModel>();
                if (!string.IsNullOrEmpty(search))
                {
                    // not supporting different ordering for now
                    var filteredPlayers = await db.PlayerSearchQuery.FromSqlInterpolated(
                        $@"SELECT * FROM (
                            SELECT *, ROW_NUMBER() OVER(ORDER BY LocalPP DESC) AS Rank
                            from Players)")
                        .Where(x=> x.Name.ToUpper() == search.ToUpper() || x.JsonName.Contains(search.ToLower()))
                        .Skip(offset)
                        .Take(limit)
                        .ToArrayAsync();

                    for (int i = 0; i < filteredPlayers.Length; i++)
                    {
                        jsonPlayers.Add(new TopPlayerModel
                        {
                            ID = filteredPlayers[i].ID,
                            JsonName = filteredPlayers[i].JsonName,
                            LivePP = filteredPlayers[i].LivePP,
                            LocalPP = filteredPlayers[i].LocalPP,
                            Name = filteredPlayers[i].Name,
                            PPLoss = Math.Round(filteredPlayers[i].PPLoss, 2),
                            Place = filteredPlayers[i].Rank
                        });
                    }
                    totalFilteredAmt = filteredPlayers.Length;
                }
                else
                {
                    var query = db.Players.AsQueryable();

                    if (!string.IsNullOrEmpty(order))
                    {
                        // this is stupid
                        sort = sort switch
                        {
                            "jsonName" => "JsonName",
                            "name" => "Name",
                            "livePP" => "LivePP",
                            "localPP" => "LocalPP",
                            "ppLoss" => "PPLoss",
                            _ => string.Empty
                        };

                        if (order == "asc")
                            query = query.OrderBy(sort);
                        else
                            query = query.OrderByDescending(sort);
                    }

                    var players = await query.Skip(offset).Take(limit).ToArrayAsync();
                    for (int i = 0; i < players.Length; i++)
                    {
                        jsonPlayers.Add(new TopPlayerModel
                        {
                            ID = players[i].ID,
                            JsonName = players[i].JsonName,
                            LivePP = players[i].LivePP,
                            LocalPP = players[i].LocalPP,
                            Name = players[i].Name,
                            PPLoss = Math.Round(players[i].PPLoss, 2),
                            Place = offset + i + 1
                        });
                    }

                    totalFilteredAmt = players.Length;
                }

                return Json(new TopModel()
                {
                    Rows = jsonPlayers.ToArray(),
                    Total = string.IsNullOrEmpty(search) ? totalNotFilteredAmt : totalFilteredAmt,
                    TotalNotFiltered = totalNotFilteredAmt
                });
            }
        }

        [HttpPost]
        [RequiresKey]
        [Route("SubmitWorkerResults")]
        public async Task<IActionResult> SubmitWorkerResults([FromBody]dynamic content, string key, string jsonUsername)
        {
            string jsonString = content.ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                dynamic json = JsonConvert.DeserializeObject(jsonString);

                await System.IO.File.WriteAllTextAsync($"players/{jsonUsername}.json", jsonString);

                await using (DatabaseContext db = new DatabaseContext())
                {
                    long userid = Convert.ToInt64(json.UserID.ToString().Split(' ')[0]);
                    string osuUsername = json.Username.ToString();
                    double livePP = Convert.ToDouble(json.LivePP.ToString().Split(' ')[0], CultureInfo.InvariantCulture);
                    double localPP = Convert.ToDouble(json.LocalPP.ToString().Split(' ')[0], CultureInfo.InvariantCulture);
                    if (await db.Players.AnyAsync(x => x.ID == userid))
                    {
                        var player = await db.Players.SingleAsync(x => x.ID == userid);
                        player.LivePP = livePP;
                        player.LocalPP = localPP;
                        player.PPLoss = localPP - livePP;
                        player.JsonName = jsonUsername;
                        player.Name = osuUsername;
                    }
                    else
                    {
                        await db.Players.AddAsync(new Player()
                        {
                            ID = userid,
                            LivePP = livePP,
                            LocalPP = localPP,
                            PPLoss = localPP - livePP,
                            Name = osuUsername,
                            JsonName = jsonUsername
                        });
                    }

                    var currentScores = await db.Scores.ToArrayAsync();
                    
                    JArray maps = json.Beatmaps;
                    var highscores = maps.Where(x => Convert.ToDouble(x["LocalPP"]) > keep_scores_bigger_than).Select(x => new Score()
                    {
                        Map = x["Beatmap"].ToString(), 
                        Player = osuUsername, 
                        PP = Convert.ToDouble(x["LocalPP"]), 
                        CalcTime = DateTime.Now.ToUniversalTime()
                    }).ToArray();

                    await db.Scores.AddRangeAsync(highscores.Except(currentScores).ToArray());

                    await db.SaveChangesAsync();

                    // one calculation a day
                    playerCache.Add(jsonUsername, jsonUsername, DateTimeOffset.Now.AddDays(1));
                }
            }

            return new OkResult();
        }

        [RequiresKey]
        [Route("GetUserForWorker")]
        public IActionResult GetUserForWorker(string key, long calcTimestamp)
        {
            if (calcTimestamp == 0)
                return StatusCode(422);

            var localCalcDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime().Ticks;

            if (calcTimestamp < localCalcDate)
            {
                // send update data
                return Json(new WorkerDataModel
                {
                    NeedsCalcUpdate = true,
                    Data = calc_update_link
                });
            }

            if (usernameQueue.TryDequeue(out var username))
                return Json(new WorkerDataModel { Data = username });

            return new OkResult();
        }

        [RequiresKey]
        [Route("LockQueue")]
        public IActionResult LockQueue(string key)
        {
            queueLocked = true;

            return new OkResult();
        }

        [RequiresKey]
        [Route("RecalcTop")]
        public async Task<IActionResult> RecalcTop(string key, int amt = 100, int offset = 0, bool force = false)
        {
            await using (DatabaseContext db = new DatabaseContext())
            {
                var players = await db.Players.OrderByDescending(x=> x.LocalPP)
                    .Skip(offset)
                    .Take(amt)
                    .Select(x => x.JsonName)
                    .ToArrayAsync();

                foreach (var player in players)
                    if (CheckFileCalcDateOutdated($"players/{player}.json") || force)
                        usernameQueue.Enqueue(player);
            }

            return new OkResult();
        }

        [HttpPost]
        [Route("CalculateMap")]
        public async Task<IActionResult> CalculateMap(CalculateMapModel model)
        {
            if (string.IsNullOrEmpty(model.Map))
                return StatusCode(400, new { err = "Incorrect beatmap link!" });

            if (!int.TryParse(model.Map, out var mapId))
            {
                mapId = GetMapIdFromLink(model.Map, out var isSet);
                if (mapId == 0)
                    return StatusCode(400, new {err = "Incorrect beatmap link!"});

                if (isSet)
                    return StatusCode(400, new {err = "Beatmap set links aren't supported"});
            }

            var modsJoined = string.Join(string.Empty, model.Mods);
            if (CheckFileCalcDateOutdated($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"))
            {
                try
                {
                    var commandMods = string.Empty;
                    if (model.Mods.Length > 0)
                        commandMods = "-m " + string.Join(" -m ", model.Mods);

                    await ProcessEx.RunAsync(new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        WorkingDirectory = workingDir,
                        Arguments =
                            $"PerformanceCalculator.dll simulate osu cache/{mapId}.osu {commandMods}",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    });

                    if (System.IO.File.Exists($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"))
                        return Json(System.IO.File.ReadAllText($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to calc {mapId}\n {e.Message}");
                }
            }
            else
            {
                return Json(System.IO.File.ReadAllText($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"));
            }

            return StatusCode(500, new { err = "Failed to calculate!" });
        }

        [Route("GetProbabilityChart")]
        public async Task<IActionResult> GetProbabilityChart(int mapId)
        {
            var graph = await System.IO.File.ReadAllLinesAsync($"cache/graph_{mapId}.txt");
            if (graph.Length > 0)
            {
                var jsonGraph = new List<ProbabilityGraphModel>(graph.Length);
                foreach (var g in graph)
                {
                    var split = g.Split(' ');
                    jsonGraph.Add(new ProbabilityGraphModel
                    {
                        Time = Convert.ToDouble(split[0]),
                        Probability = Convert.ToDouble(split[3])
                    });
                }

                return Json(JsonConvert.SerializeObject(jsonGraph));
            }

            return StatusCode(400);
        }

        [Route("GetHighscores")]
        public async Task<IActionResult> GetHighscores()
        {
            await using var db = new DatabaseContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var calcUpdateDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime();
            return Json(await db.Scores.Where(x => x.CalcTime > calcUpdateDate).OrderByDescending(x=> x.PP).ToArrayAsync());
        }

        [RequiresKey]
        [Route("ClearHighscores")]
        public async Task<IActionResult> ClearHighscores(string key)
        {
            using var db = new DatabaseContext();
            db.Scores.RemoveRange(await db.Scores.Select(x => x).ToArrayAsync());
            await db.SaveChangesAsync();

            return new OkResult();
        }

        [RequiresKey]
        [Route("RemovePlayer")]
        public async Task<IActionResult> RemovePlayer(string key, string name)
        {
            using var db = new DatabaseContext();

            db.Players.Remove(await db.Players.SingleAsync(x => x.Name == name));
            db.Scores.RemoveRange(await db.Scores.Where(x => x.Player == name).ToArrayAsync());
            await db.SaveChangesAsync();

            return new OkResult();
        }


        #region Helpers

        private bool CheckFileCalcDateOutdated(string path)
        {
            if (!System.IO.File.Exists(path))
                return true;

            var fileCalcDate = System.IO.File.GetLastWriteTime(path).ToUniversalTime();
            var calcUpdateDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime();
            if (fileCalcDate < calcUpdateDate)
                return true;

            return false;
        }

        private int GetMapIdFromLink(string link, out bool isSet)
        {
            int beatmapId = 0;
            isSet = false;
            Match regexMatch = mapLinkRegex.Match(link);
            if (regexMatch.Groups.Count > 1)
            {
                List<Group> regexGroups = regexMatch.Groups.Values
                    .Where(x => x != null && x.Length > 0)
                    .ToList();

                bool isNew = regexGroups[1].Value == "beatmapsets"; // are we using new website or not
                if (isNew)
                {
                    if (regexGroups[2].Value.Contains("#osu/"))
                        beatmapId = int.Parse(regexGroups[3].Value);
                    else
                        isSet = true;
                }
                else
                {
                    if (regexGroups[1].Value == "s")
                        isSet = true;
                    else
                        beatmapId = int.Parse(regexGroups[2].Value);
                }
            }

            return beatmapId;
        }

        #endregion

    }
}
