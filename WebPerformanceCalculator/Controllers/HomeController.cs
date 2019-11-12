
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
    public class HomeController : Controller
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

        public HomeController()
        {
            var assemblyFileInfo = new FileInfo(typeof(Program).Assembly.Location);
            workingDir = assemblyFileInfo.DirectoryName;
        }

        #region Pages

        public IActionResult Index()
        {
            var date = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime();
            return View(model: $"{date.ToString(CultureInfo.InvariantCulture)} UTC ({TimeAgo(date)})");
        }

        public IActionResult Top()
        {
            return View();
        }

        public IActionResult User(string username)
        {
            var updateDateString = string.Empty;

            if (System.IO.File.Exists($"players/{username}.json"))
            {
                var fileDate = System.IO.File.GetLastWriteTime($"players/{username}.json").ToUniversalTime();
                if (CheckFileCalcDateOutdated($"players/{username}.json"))
                    updateDateString = $"{fileDate.ToString(CultureInfo.InvariantCulture)} UTC (outdated!)";
                else
                    updateDateString = $"{fileDate.ToString(CultureInfo.InvariantCulture)} UTC";
            }

            return View(new UserModel
            {
                Username = username,
                UpdateDate = updateDateString
            });
        }

        [Route("Map/{id?}")]
        public IActionResult Map(int? id = null)
        {
            return View(model: id?.ToString() ?? string.Empty);
        }

        public IActionResult Highscores()
        {
            return View();
        }

        #endregion

        #region API

        [HttpPost]
        public IActionResult AddToQueue(string jsonUsername)
        {
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

        public IActionResult GetQueue()
        {
            return Json(usernameQueue.ToArray());
        }

        public async Task<IActionResult> GetResults(string jsonUsername)
        {
            var result = string.Empty;
            if (System.IO.File.Exists($"players/{jsonUsername}.json"))
                result = await System.IO.File.ReadAllTextAsync($"players/{jsonUsername}.json");

            return Json(result);
        }

        public async Task<IActionResult> GetTop(int offset = 0, int limit = 50, string search = null, string order = "asc", string sort = "localPP")
        {
            await using (DatabaseContext db = new DatabaseContext())
            {
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var totalNotFilteredAmt = await db.Players.CountAsync();

                var query = db.Players.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.JsonName.Contains(search.ToLower()) || x.Name.Contains(search));

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

                var jsonPlayers = new List<TopPlayerModel>();
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

                return Json(new TopModel()
                {
                    Rows = jsonPlayers.ToArray(),
                    Total = string.IsNullOrEmpty(search) ? totalNotFilteredAmt : players.Length,
                    TotalNotFiltered = totalNotFilteredAmt
                });
            }
        }

        [HttpPost]
        [RequiresKey]
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
        public IActionResult LockQueue(string key)
        {
            queueLocked = true;

            return new OkResult();
        }

        [RequiresKey]
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

        public async Task<IActionResult> CalculateMap(string map, string[] mods)
        {
            if (!int.TryParse(map, out var mapId))
            {
                mapId = GetMapIdFromLink(map, out var isSet);
                if (mapId == 0)
                    return StatusCode(500, new {err = "Incorrect beatmap link!"});

                if (isSet)
                    return StatusCode(500, new {err = "Beatmap set links aren't supported"});
            }

            var modsJoined = string.Join(string.Empty, mods);
            if (CheckFileCalcDateOutdated($"{workingDir}/mapinfo/{mapId}_{modsJoined}.json"))
            {
                try
                {
                    var commandMods = string.Empty;
                    if (mods.Length > 0)
                        commandMods = "-m " + string.Join(" -m ", mods);

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

        public async Task<IActionResult> GetHighscores()
        {
            await using var db = new DatabaseContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var calcUpdateDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime();
            return Json(await db.Scores.Where(x => x.CalcTime > calcUpdateDate).OrderByDescending(x=> x.PP).ToArrayAsync());
        }

        [RequiresKey]
        public IActionResult ClearHighscores(string key)
        {
            using var db = new DatabaseContext();
            db.Scores.RemoveRange(db.Scores.Select(x => x));
            db.SaveChanges();

            return new OkResult();
        }

        [RequiresKey]
        public IActionResult RemovePlayer(string key, string name)
        {
            using var db = new DatabaseContext();

            db.Players.Remove(db.Players.Single(x => x.Name == name));
            db.Scores.RemoveRange(db.Scores.Where(x => x.Player == name));
            db.SaveChanges();

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

        private string TimeAgo(DateTime dt)
        {
            if (dt > DateTime.Now.ToUniversalTime())
                return "soon";
            TimeSpan span = DateTime.Now.ToUniversalTime() - dt;

            switch (span)
            {
                case var _ when span.Days > 365:
                    {
                        int years = (span.Days / 365);
                        if (span.Days % 365 != 0)
                            years += 1;
                        return $"about {years} {(years == 1 ? "year" : "years")} ago";
                    }
                case var _ when span.Days > 30:
                    {
                        int months = (span.Days / 30);
                        if (span.Days % 31 != 0)
                            months += 1;
                        return $"about {months} {(months == 1 ? "month" : "months")} ago";
                    }

                case var _ when span.Days > 0:
                    return $"about {span.Days} {(span.Days == 1 ? "day" : "days")} ago";

                case var _ when span.Hours > 0:
                    return $"about {span.Hours} {(span.Hours == 1 ? "hour" : "hours")} ago";

                case var _ when span.Minutes > 0:
                    return $"about {span.Minutes} {(span.Minutes == 1 ? "minute" : "minutes")} ago";

                case var _ when span.Seconds > 5:
                    return $"about {span.Seconds} seconds ago";

                case var _ when span.Seconds <= 5:
                    return "just now";

                default: return string.Empty;
            }
        }

        #endregion

    }
}
