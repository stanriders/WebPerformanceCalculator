
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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

        private const string calc_file = "osu.Game.Rulesets.Osu.dll";
        private const string calc_update_link = "http://dandan.kikoe.ru/osu.Game.Rulesets.Osu.dll";

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
                if (CheckUserCalcDate(username))
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

        private static string TimeAgo(DateTime dt)
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

        #region API

        [HttpPost]
        public IActionResult AddToQueue(string jsonUsername)
        {
            if (queueLocked)
                return StatusCode(500, new { err = "Queue is temporary locked, try again later!" });

            if (queueDebounce > DateTime.Now)
                return StatusCode(500, new {err = "Try again later"});

            // performance calculator doesn't want to work with them even when escaping
            if (jsonUsername.Contains('-'))
                return StatusCode(500, new {err = "Please use user ID instead"});

            jsonUsername = jsonUsername.Trim();

            var regexp = new Regex(@"^[A-Za-z0-9-\[\]_ ]+$");

            if (jsonUsername.Length > 2 && jsonUsername.Length < 16 && regexp.IsMatch(jsonUsername))
            {
                jsonUsername = HttpUtility.HtmlEncode(jsonUsername).ToLowerInvariant();
                if (!usernameQueue.Contains(jsonUsername) && CheckUserCalcDate(jsonUsername))
                {
                    usernameQueue.Enqueue(jsonUsername);
                    queueDebounce = DateTime.Now.AddSeconds(1);
                    return GetQueue();
                }

                return StatusCode(500, new { err = "Recalculation is only allowed after formula updates!" });
            }

            return StatusCode(500, new {err = "Incorrect username"});
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
        public async Task<IActionResult> SubmitWorkerResults([FromBody]dynamic content, string key, string jsonUsername)
        {
            if (key != Config.auth_key)
                return StatusCode(403);

            string jsonString = content.ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                var json = JObject.Parse(jsonString);

                await System.IO.File.WriteAllTextAsync($"players/{jsonUsername}.json", jsonString);

                await using (DatabaseContext db = new DatabaseContext())
                {
                    var userid = Convert.ToInt64(json["UserID"].ToString().Split(' ')[0]);
                    var osuUsername = json["Username"].ToString();
                    var livePP = Convert.ToDouble(json["LivePP"].ToString().Split(' ')[0], CultureInfo.InvariantCulture);
                    var localPP = Convert.ToDouble(json["LocalPP"].ToString().Split(' ')[0], CultureInfo.InvariantCulture);
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

                    await db.SaveChangesAsync();
                }
            }

            return new OkResult();
        }

        public IActionResult GetUserForWorker(string key, long calcTimestamp)
        {
            if (key != Config.auth_key || calcTimestamp == 0)
                return StatusCode(403);

            var localCalcDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime().Ticks;

            if (calcTimestamp < localCalcDate)
            {
                return Json(new WorkerDataModel
                {
                    NeedsCalcUpdate = true,
                    Data = calc_update_link
                });
            }

            if (usernameQueue.TryDequeue(out var username))
                return Json(new WorkerDataModel() { Data = username });

            return new OkResult();
        }

        public IActionResult LockQueue(string key)
        {
            if (key != Config.auth_key)
                return StatusCode(403);

            queueLocked = true;

            return new OkResult();
        }

        public async Task<IActionResult> RecalcTop(string key, int amt = 100, int offset = 0)
        {
            if (key != Config.auth_key)
                return StatusCode(403);

            await using (DatabaseContext db = new DatabaseContext())
            {
                var players = await db.Players.OrderByDescending(x=> x.LocalPP)
                    .Skip(offset)
                    .Take(amt)
                    .Select(x => x.JsonName)
                    .ToArrayAsync();

                foreach (var player in players)
                    if (!CheckUserCalcDate(player))
                        usernameQueue.Enqueue(player);
            }

            return new OkResult();
        }

        #endregion

        private static bool CheckUserCalcDate(string jsonUsername)
        {
            if (!System.IO.File.Exists($"players/{jsonUsername}.json"))
                return true;

            var userCalcDate = System.IO.File.GetLastWriteTime($"players/{jsonUsername}.json").ToUniversalTime();
            var calcUpdateDate = System.IO.File.GetLastWriteTime(calc_file).ToUniversalTime();
            if (userCalcDate < calcUpdateDate)
                return true;

            return false;
        }
    }
}
