
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPerformanceCalculator.Attributes;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.Services;

namespace WebPerformanceCalculator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly PlayerQueueService playerQueue;
        private readonly CalculationUpdatesService updateService;

        public AdminController(PlayerQueueService _playerQueue, CalculationUpdatesService _updateService)
        {
            playerQueue = _playerQueue;
            updateService = _updateService;
        }

        [RequiresKey]
        [Route("ToggleQueue")]
        public IActionResult ToggleQueue(string key)
        {
            playerQueue.ToggleQueue();
            return new OkResult();
        }

        [RequiresKey]
        [Route("ClearQueue")]
        public IActionResult ClearQueue(string key)
        {
            playerQueue.ClearQueue();
            return new OkResult();
        }

        [RequiresKey]
        [Route("RecalcTop")]
        public async Task<IActionResult> RecalcTop(string key, int amt = 100, int offset = 0, bool force = false)
        {
            await using var db = new DatabaseContext();

            var players = await db.Players.OrderByDescending(x => x.LocalPp)
                .Skip(offset)
                .Take(amt)
                .ToArrayAsync();

            foreach (var player in players)
                if (updateService.IsOutdated(player.UpdateTime) || force)
                    playerQueue.AddToQueueInternal(player.Id.ToString());

            return new OkResult();
        }

        [RequiresKey]
        [Route("ClearHighscores")]
        public async Task<IActionResult> ClearHighscores(string key)
        {
            playerQueue.ClearCaches();

            await using var db = new DatabaseContext();
            db.Scores.RemoveRange(await db.Scores.Select(x => x).ToArrayAsync());
            await db.SaveChangesAsync();

            var players = await db.Players.OrderByDescending(x => x.LocalPp)
                .Take(100)
                .Select(x => x.Id.ToString())
                .ToArrayAsync();

            foreach (var player in players)
                playerQueue.AddToQueueInternal(player);

            return new OkResult();
        }

        [RequiresKey]
        [Route("RemovePlayer")]
        public async Task<IActionResult> RemovePlayer(string key, string name)
        {
            await using var db = new DatabaseContext();

            var player = db.Players.FirstOrDefaultAsync(x => x.Name == name);
            if (player != null)
            {
                db.Players.Remove(await db.Players.SingleAsync(x => x.Name == name));
                db.Scores.RemoveRange(await db.Scores.Where(x => x.PlayerId == player.Id).ToArrayAsync());
            }

            await db.SaveChangesAsync();

            return new OkResult();
        }

        [RequiresKey]
        [Route("GetUsers")]
        public IActionResult GetUsers(string key)
        {
            return new JsonResult(playerQueue.GetUsersStats());
        }
    }
}
