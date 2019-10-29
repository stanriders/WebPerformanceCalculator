
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.Models;

namespace WebPerformanceCalculator.Controllers
{
    public class HomeController : Controller
    {
        private static DateTime queueDebounce = DateTime.Now;
        private static string workingDir;
        private static bool queueLocked;

        private const string auth_key = "";

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(workingDir))
            {
                var assemblyFileInfo = new FileInfo(typeof(HomeController).Assembly.Location);
                workingDir = assemblyFileInfo.DirectoryName;
            }

            var date = System.IO.File.GetLastWriteTime($"{workingDir}/osu.Game.Rulesets.Osu.dll");
            return View(model: date.ToUniversalTime().ToString(CultureInfo.InvariantCulture));
        }

        public IActionResult Top()
        {
            return View();
        }

        public IActionResult User(string username)
        {
            var updateDateString = string.Empty;

            var calcDate = System.IO.File.GetLastWriteTime($"{workingDir}/osu.Game.Rulesets.Osu.dll").ToUniversalTime();

            if (System.IO.File.Exists($"{workingDir}/players/{username}.json"))
            {
                var fileDate = System.IO.File.GetLastWriteTime($"{workingDir}/players/{username}.json")
                    .ToUniversalTime();
                if (fileDate < calcDate)
                    updateDateString = $"{fileDate.ToString(CultureInfo.InvariantCulture)} UTC (outdated!)";
                else
                    updateDateString = $"{fileDate.ToString(CultureInfo.InvariantCulture)} UTC";
            }

            return View(model: new UserModel
            {
                Username = username,
                UpdateDate = updateDateString
            });
        }

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

            if (jsonUsername.Length > 2 && jsonUsername.Length < 15 && regexp.IsMatch(jsonUsername))
            {
                if (CalcQueue.AddToQueue(HttpUtility.HtmlEncode(jsonUsername)))
                {
                    queueDebounce = DateTime.Now.AddSeconds(1);
                    return GetQueue();
                }
                else
                    return StatusCode(500, new {err = "This player doesn't need a recalc yet!"});
            }

            return StatusCode(500, new {err = "Incorrect username"});
        }

        public IActionResult GetQueue()
        {
            return Json(CalcQueue.GetQueue());
        }

        public async Task<IActionResult> GetResults(string jsonUsername)
        {
            var result = string.Empty;
            if (System.IO.File.Exists($"{workingDir}/players/{jsonUsername}.json"))
                result = await System.IO.File.ReadAllTextAsync($"{workingDir}/players/{jsonUsername}.json");

            return Json(result);
        }

        public async Task<IActionResult> GetTop()
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var players = (await db.Players.ToArrayAsync()).OrderByDescending(x => x.LocalPP).ToArray();
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
                        PPLoss = players[i].PPLoss,
                        Place = i + 1
                    });
                }

                return Json(jsonPlayers);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitWorkerResults([FromBody]dynamic content, string key, string jsonUsername)
        {
            if (key != auth_key)
                return StatusCode(403);

            string jsonString = content.ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                var json = JObject.Parse(jsonString);

                await System.IO.File.WriteAllTextAsync($"{workingDir}/players/{jsonUsername}.json", jsonString);

                using (DatabaseContext db = new DatabaseContext())
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

            // we want to remove user from queue even if processing failed
            CalcQueue.RemoveFromProcessing(jsonUsername);
            return new OkResult();
        }

        public IActionResult GetUserForWorker(string key)
        {
            if (key != auth_key)
                return StatusCode(403);

            var username = CalcQueue.GetUserForCalc();
            if (!string.IsNullOrEmpty(username))
                return Json(new { user = username });

            return new OkResult();
        }

        public IActionResult LockQueue(string key)
        {
            if (key != auth_key)
                return StatusCode(403);

            queueLocked = true;

            return new OkResult();
        }
    }
}
