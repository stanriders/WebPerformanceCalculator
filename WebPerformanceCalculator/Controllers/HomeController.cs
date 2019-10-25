
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebPerformanceCalculator.DB;

namespace WebPerformanceCalculator.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Top()
        {
            return View();
        }

        public IActionResult User(string username)
        {
            return View(model: username);
        }

        [HttpPost]
        public IActionResult AddToQueue(string jsonUsername)
        {
            CalcQueue.AddToQueue(jsonUsername);
            return GetQueue();
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
