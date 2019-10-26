﻿
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.Models;

namespace WebPerformanceCalculator.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            var assemblyFileInfo = new FileInfo(typeof(HomeController).Assembly.Location);
            var workingDir = assemblyFileInfo.DirectoryName;
            var date = System.IO.File.GetLastWriteTime($"{workingDir}/osu.Game.Rulesets.Osu.dll");
            return View(model: date.ToUniversalTime().ToString());
        }

        public IActionResult Top()
        {
            return View();
        }

        public IActionResult User(string username)
        {
            var assemblyFileInfo = new FileInfo(typeof(HomeController).Assembly.Location);
            var workingDir = assemblyFileInfo.DirectoryName;

            var updateDateString = string.Empty;

            var calcDate = System.IO.File.GetLastWriteTime($"{workingDir}/osu.Game.Rulesets.Osu.dll").ToUniversalTime();

            if (System.IO.File.Exists($"{workingDir}/{username}.json"))
            {
                var fileDate = System.IO.File.GetLastWriteTime($"{workingDir}/{username}.json").ToUniversalTime();
                if (fileDate < calcDate)
                    updateDateString = $"{fileDate.ToString()} UTC (outdated!)";
                else
                    updateDateString = $"{fileDate.ToString()} UTC";
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
            if (jsonUsername.Length <= 24)
            {
                if (CalcQueue.AddToQueue(HttpUtility.HtmlEncode(jsonUsername)))
                    return GetQueue();
                else
                    return StatusCode(500, new { err = "Try again later" });
            }
            return StatusCode(500, new { err = "Incorrect username" });
        }

        public IActionResult GetQueue()
        {
            return Json(JsonConvert.SerializeObject(CalcQueue.GetQueued()));
        }

        public async Task<IActionResult> GetResults(string jsonUsername)
        {
            var workingDir = new FileInfo(typeof(HomeController).Assembly.Location).DirectoryName;

            var result = string.Empty;
            if (System.IO.File.Exists($"{workingDir}/{jsonUsername}.json"))
                result = await System.IO.File.ReadAllTextAsync($"{workingDir}/{jsonUsername}.json");

            return Json(result);
        }

        public async Task<IActionResult> GetTop()
        {
            using (DatabaseContext db = new DatabaseContext())
            {
                db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var players = await db.Players.ToArrayAsync();
                var json = JsonConvert.SerializeObject(players.OrderByDescending(x=> x.LocalPP));
                return Json(json);
            }
        }
    }
}
