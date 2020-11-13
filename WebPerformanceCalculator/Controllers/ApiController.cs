
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
        private static string commitHash = "unknown";

        private static readonly Regex mapLinkRegex = 
            new Regex(@"(?>https?:\/\/)?(?>osu|old)\.ppy\.sh\/([b,s]|(?>beatmaps)|(?>beatmapsets))\/(\d+\/?\#osu\/)?(\d+)?\/?(?>[&,?].=\d)?", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const double keep_scores_bigger_than = 600.0;

        private const string commit_hash_file = "commithash";
        private const string calc_file = "osu.Game.Rulesets.Osu.dll";

        public ApiController()
        {
            var assemblyFileInfo = new FileInfo(typeof(Program).Assembly.Location);
            workingDir = assemblyFileInfo.DirectoryName;

            if (System.IO.File.Exists(commit_hash_file))
                commitHash = System.IO.File.ReadAllText(commit_hash_file);
        }

        [Route("GetCalcModuleUpdateDate")]
        public async Task<IActionResult> GetCalcModuleUpdateDate()
        {
            if (System.IO.File.Exists(commit_hash_file))
            {
                var fileHash = await System.IO.File.ReadAllTextAsync(commit_hash_file);
                commitHash = fileHash;
            }

            return Json(new
            {
                date = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime(),
                commit = commitHash
            });
        }

        #region /

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

            if (jsonUsername.Contains("_full"))
                return StatusCode(400, new { err = "Incorrect username" });

            jsonUsername = jsonUsername.Trim();

            var regexp = new Regex(@"^[A-Za-z0-9-\[\]_ ]+$");

            if (jsonUsername.Length > 2 && jsonUsername.Length < 16 && regexp.IsMatch(jsonUsername))
            {
                jsonUsername = HttpUtility.HtmlEncode(jsonUsername).ToLowerInvariant();
                if (!usernameQueue.Contains(jsonUsername))
                {
                    usernameQueue.Enqueue(jsonUsername);
                    queueDebounce = DateTime.Now.AddSeconds(1);
                    return GetQueue();
                }

                return StatusCode(400, new { err = "This player is already in queue" });
            }

            return StatusCode(400, new {err = "Incorrect username"});
        }

        [Route("GetQueue")]
        public IActionResult GetQueue()
        {
            return Json(usernameQueue.ToArray());
        }

        #endregion

        #region /user

        [Route("GetResults")]
        public async Task<IActionResult> GetResults(string player)
        {
            if (string.IsNullOrEmpty(player))
                return Json(new { Username = "Incorrect username" });

            if (System.IO.File.Exists($"players/{player}.json"))
            {
                var json = JsonConvert.DeserializeObject<PlayerModel>(await System.IO.File.ReadAllTextAsync($"players/{player}.json"));
                json.UpdateDate = System.IO.File.GetLastWriteTime($"players/{player}.json").ToUniversalTime();
                return Json(json);
            }
            else
            {
                using (var dbContext = new DatabaseContext())
                {
                    var dbPlayer = await dbContext.Players.FirstOrDefaultAsync(x => x.Name.ToUpper() == player.ToUpper());
                    if (dbPlayer != null)
                        return RedirectPermanent($"/api/GetResults?player={dbPlayer.JsonName}");
                    
                    return Json(new { Username = "Unknown player" });
                }
            }
        }

        #endregion

        #region /top, /countrytop

        [Route("GetTop")]
        public async Task<IActionResult> GetTop(int offset = 0, int limit = 50, string search = null, string order = "desc", string sort = "localPP", string country = null)
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var totalNotFilteredAmt = 0;
                if (string.IsNullOrEmpty(country))
                    totalNotFilteredAmt = await db.Players.CountAsync();
                else
                    totalNotFilteredAmt = await db.Players.Where(x=>x.Country == country.ToUpperInvariant()).CountAsync();

                List<TopPlayerModel> jsonPlayers;
                if (!string.IsNullOrEmpty(search))
                {
                    // not supporting different ordering for now
                    jsonPlayers = await db.PlayerSearchQuery.FromSql(
                        @"SELECT * FROM (
                            SELECT *, ROW_NUMBER() OVER(ORDER BY LocalPP DESC) AS Rank
                            from Players)")
                        .Where(x=> x.Name.ToUpper().Contains(search.ToUpper()) || x.JsonName.Contains(search.ToLower()))
                        .Skip(offset)
                        .Take(limit)
                        .Select(x => new TopPlayerModel
                        {
                            ID = x.ID,
                            JsonName = x.JsonName,
                            LivePP = x.LivePP,
                            LocalPP = x.LocalPP,
                            Name = x.Name,
                            PPLoss = Math.Round(x.PPLoss, 2),
                            Place = x.Rank,
                            Country = x.Country
                        }).ToListAsync();
                }
                else
                {
                    var query = db.Players.AsQueryable();

                    if (!string.IsNullOrEmpty(country))
                        query = query.Where(x => x.Country == country.ToUpperInvariant());

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

                    jsonPlayers = new List<TopPlayerModel>(players.Length);
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (!CheckFileCalcDateOutdated($"players/{players[i].JsonName}.json"))
                        {
                            jsonPlayers.Add(new TopPlayerModel
                            {
                                ID = players[i].ID,
                                JsonName = players[i].JsonName,
                                LivePP = players[i].LivePP,
                                LocalPP = players[i].LocalPP,
                                Name = players[i].Name,
                                PPLoss = Math.Round(players[i].PPLoss, 2),
                                Place = offset + i + 1,
                                Country = players[i].Country
                            });
                        }
                        else
                        {
                            jsonPlayers.Add(new TopPlayerModel
                            {
                                ID = players[i].ID,
                                JsonName = players[i].JsonName,
                                LivePP = players[i].LivePP,
                                LocalPP = 0,
                                Name = players[i].Name,
                                PPLoss = 0.0,
                                Place = offset + i + 1,
                                Country = players[i].Country
                            });
                        }
                    }
                }

                return Json(new TopModel()
                {
                    Rows = jsonPlayers.ToArray(),
                    Total = string.IsNullOrEmpty(search) ? totalNotFilteredAmt : jsonPlayers.Count,
                    TotalNotFiltered = totalNotFilteredAmt
                });
            }
        }

        #endregion

        #region /map

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
                        return Ok(System.IO.File.ReadAllText($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to calc {mapId}\n {e.Message}");
                }
            }
            else
            {
                return Ok(System.IO.File.ReadAllText($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"));
            }

            return StatusCode(500, new { err = "Failed to calculate!" });
        }

        [Route("GetProbabilityChart")]
        public async Task<IActionResult> GetProbabilityChart(string mapId, string mods = "")
        {
            if (mods == null)
                mods = string.Empty;

            if (System.IO.File.Exists($"cache/graph_{mapId}_{mods}.txt"))
            {
                var graph = await System.IO.File.ReadAllLinesAsync($"cache/graph_{mapId}_{mods}.txt");
                if (graph.Length > 0)
                {
                    var probGraph = new List<ProbabilityGraphModel>(graph.Length);
                    var ipGraph = new List<IndexGraphModel>(graph.Length);
                    var fingerGraph = new List<FingerGraphModel>();
                    var tapGraph = new List<TapGraphModel>();
                    foreach (var g in graph)
                    {
                        var split = g.Split(' ');
                        probGraph.Add(new ProbabilityGraphModel
                        {
                            Time = Convert.ToDouble(split[0]),
                            Probability = Convert.ToDouble(split[3])
                        });

                        ipGraph.Add(new IndexGraphModel
                        {
                            Time = Convert.ToDouble(split[0]),
                            IPCorrected = Convert.ToDouble(split[2])
                        });
                    }

                    var finger = await System.IO.File.ReadAllLinesAsync($"cache/graph_{mapId}_{mods}_finger.txt");
                    if (finger.Length > 0)
                    {
                        foreach (var g in finger)
                        {
                            var split = g.Split(' ');
                            fingerGraph.Add(new FingerGraphModel
                            {
                                Time = Convert.ToDouble(split[0]),
                                Difficulty = Convert.ToDouble(split[2])
                            });
                        }
                    }

                    var tap = await System.IO.File.ReadAllLinesAsync($"cache/graph_{mapId}_{mods}_tap.txt");
                    if (tap.Length > 0)
                    {
                        foreach (var g in tap)
                        {
                            var split = g.Split(' ');
                            tapGraph.Add(new TapGraphModel
                            {
                                Time = Convert.ToDouble(split[0]),
                                Difficulty = Convert.ToDouble(split[5])
                            });
                        }
                    }
                    return Ok(new { probGraph, ipGraph, fingerGraph, tapGraph });
                }
            }

            return StatusCode(400);
        }

        #endregion

        #region /highscores

        [Route("GetHighscores")]
        public async Task<IActionResult> GetHighscores()
        {
            using var db = new DatabaseContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var calcUpdateDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime();
            return Json(await db.Scores.Where(x => x.CalcTime > calcUpdateDate).OrderByDescending(x=> x.PP).ToArrayAsync());
        }

        #endregion

        #region Workers

        [HttpPost]
        [RequiresKey]
        [Route("SubmitWorkerResults")]
        public async Task<IActionResult> SubmitWorkerResults([FromBody]dynamic content, string key, string jsonUsername)
        {
            string jsonString = content.ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                var json = JsonConvert.DeserializeObject<PlayerModel>(jsonString); 
                long userid = Convert.ToInt64(json.UserID.ToString().Split(' ')[0]);

                await System.IO.File.WriteAllTextAsync($"players/{userid}.json", jsonString);

                using (DatabaseContext db = new DatabaseContext())
                {
                    string osuUsername = json.Username;
                    string country = json.UserCountry;
                    double livePP = Convert.ToDouble(json.SitePP.Split(' ')[0], CultureInfo.InvariantCulture);
                    double localPP = Convert.ToDouble(json.LocalPP.Split(' ')[0], CultureInfo.InvariantCulture);
                    if (await db.Players.AnyAsync(x => x.ID == userid))
                    {
                        var player = await db.Players.SingleAsync(x => x.ID == userid);
                        player.LivePP = livePP;
                        player.LocalPP = localPP;
                        player.PPLoss = localPP - livePP;
                        player.JsonName = userid.ToString(); // TODO: remove;
                        player.Name = osuUsername;
                        player.Country = country;
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
                            JsonName = userid.ToString(), // TODO: remove,
                            Country = country
                        });
                    }

                    var currentScores = await db.Scores.ToArrayAsync();

                    var maps = json.Beatmaps;
                    var highscores = maps.Where(x => Convert.ToDouble(x.LocalPP) > keep_scores_bigger_than).Select(x => new Score()
                    {
                        Map = x.Beatmap,
                        Player = osuUsername,
                        PP = Convert.ToDouble(x.LocalPP),
                        CalcTime = DateTime.Now.ToUniversalTime(),
                        LivePP = Convert.ToDouble(x.LivePP),
                        JsonName = userid.ToString() // TODO: remove
                    }).ToArray();

                    await db.Scores.AddRangeAsync(highscores.Except(currentScores).ToArray());

                    await db.SaveChangesAsync();
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

            if (usernameQueue.TryDequeue(out var username))
                return Json(new WorkerDataModel { Data = username });

            return new OkResult();
        }

        #endregion

        #region Commands

        [RequiresKey]
        [Route("ToggleQueue")]
        public IActionResult ToggleQueue(string key)
        {
            queueLocked = !queueLocked;
            return new OkResult();
        }

        [RequiresKey]
        [Route("RecalcTop")]
        public async Task<IActionResult> RecalcTop(string key, int amt = 100, int offset = 0, bool force = false)
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                var players = await db.Players.OrderByDescending(x => x.LocalPP)
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

        [RequiresKey]
        [Route("ClearHighscores")]
        public async Task<IActionResult> ClearHighscores(string key, bool noRecalc = false)
        {
            using var db = new DatabaseContext();
            db.Scores.RemoveRange(await db.Scores.Select(x => x).ToArrayAsync());
            await db.SaveChangesAsync();

            if (!noRecalc)
            {
                var players = db.Players.OrderByDescending(x => x.LocalPP)
                    .Take(25)
                    .Select(x => x.JsonName)
                    .ToArray();

                foreach (var player in players)
                    usernameQueue.Enqueue(player);

            }

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

        #endregion

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
