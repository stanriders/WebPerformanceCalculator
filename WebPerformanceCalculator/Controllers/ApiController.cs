
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.Models;
using WebPerformanceCalculator.Services;

namespace WebPerformanceCalculator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly double highscoreThreshold; // pp threshold for /highscores/

        private readonly IMapper mapper;
        private readonly PlayerQueueService playerQueue;
        private readonly CalculationUpdatesService updateService;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ApiController(IMapper _mapper, 
            PlayerQueueService _playerQueue, 
            CalculationUpdatesService _updateService, 
            IConfiguration _configuration, 
            IHttpContextAccessor _httpContextAccessor)
        {
            if (!double.TryParse(_configuration["HighscoreThreshold"], out highscoreThreshold))
                highscoreThreshold = 699.5;

            mapper = _mapper;
            playerQueue = _playerQueue;
            updateService = _updateService;
            httpContextAccessor = _httpContextAccessor;
        }

        [Route("version")]
        public IActionResult GetVersion()
        {
            return new JsonResult(new
            {
                date = updateService.CalculationModuleUpdateTime,
                commit = updateService.CommitHash
            });
        }

        [HttpPost]
        [Route("queue")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult AddToQueue([FromForm] string player)
        {
            var (success, errorMessage) = playerQueue.AddToQueue(player, httpContextAccessor.GetIpAddress());
            if (!success)
                return StatusCode(400, new {err = errorMessage});

            return new JsonResult(playerQueue.GetQueue());
        }

        [Route("queue")]
        public IActionResult GetQueue()
        {
            return new JsonResult(playerQueue.GetQueue());
        }

        [Route("calculating")]
        public IActionResult GetCalculatingPlayers()
        {
            return new JsonResult(playerQueue.GetCalculatingPlayers());
        }

        [HttpGet("player/{name}")]
        [Route("player")]
        public async Task<IActionResult> GetResults(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new JsonResult(new {Username = "Incorrect username"});

            await using var db = new DatabaseContext();

            if (int.TryParse(name, out var playerId))
            {
                var dbPlayer = await db.Players.AsNoTracking().FirstOrDefaultAsync(x => x.Id == playerId);
                if (dbPlayer != null)
                {
                    var model = mapper.Map<PlayerModel>(dbPlayer);
                    model.Scores = await db.Scores.AsNoTracking()
                        .Where(x => x.PlayerId == playerId)
                        .OrderByDescending(x => x.LocalPp)
                        .Include(x => x.Map)
                        .ToListAsync();

                    model.Outdated = updateService.IsOutdated(model.UpdateTime);
                    return new JsonResult(model);
                }
            }
            else
            {
                var dbPlayerId = await db.Players.Where(x => x.Name.ToUpper() == name.ToUpper()).Select(x => x.Id)
                    .FirstOrDefaultAsync();
                if (dbPlayerId != default)
                    return RedirectPermanent($"/api/GetResults?player={dbPlayerId}");
            }

            return new JsonResult(new {Username = "Unknown player"});
        }

        [Route("top")]
        public async Task<IActionResult> GetTop(int offset = 0, int limit = 50, string? search = null, string? order = "desc", string? sort = "localPp", string? country = null)
        {
            await using var db = new DatabaseContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var query = db.PlayerSearchQuery.FromSqlRaw(
                @"SELECT *, 
                            ROW_NUMBER() OVER(ORDER BY LocalPp DESC) AS Rank, 
                            ROW_NUMBER() OVER(ORDER BY LivePp DESC) AS LiveRank
                            from Players").AsQueryable();

            var totalNotFilteredAmt = 0;
            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(x => x.Country == country.ToUpperInvariant());
                totalNotFilteredAmt = await db.Players.Where(x => x.Country == country.ToUpperInvariant()).CountAsync();
            }
            else
            {
                totalNotFilteredAmt = await db.Players.CountAsync();
            }

            if (!string.IsNullOrEmpty(order))
            {
                sort = FixupSort(sort);

                query = order == "asc" ? query.OrderByMember(sort) : query.OrderByMemberDescending(sort);
            }

            if (!string.IsNullOrEmpty(search))
            {
                if (int.TryParse(search, out var id))
                    query = query.Where(x => x.Id == id);
                else
                    query = query.Where(x => x.Name.ToUpper().Contains(search.ToUpper()));
            }

            // clamp limit
            if (limit > 500)
                limit = 500;

            var players = await query
                .Skip(offset)
                .Take(limit)
                .Select(x=> new TopPlayerModel
                {
                     Id = x.Id,
                     Name = x.Name,
                     LivePp = x.LivePp,
                     LocalPp = x.LocalPp,
                     Country = x.Country,
                     LivePlace = x.LiveRank,
                     Place = x.Rank,
                     RankChange = x.Rank - x.LiveRank
                })
                .ToArrayAsync();

            return new JsonResult(new PaginationModel<TopPlayerModel>
            {
                Rows = players,
                Total = string.IsNullOrEmpty(search) ? totalNotFilteredAmt : players.Length,
                TotalNotFiltered = totalNotFilteredAmt
            });
        }

        [Route("countries")]
        public async Task<IActionResult> GetCountries()
        {
            await using var db = new DatabaseContext();

            var countries = await db.Players.AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.Country))
                .Select(x => x.Country)
                .Distinct()
                .ToArrayAsync();

            return new JsonResult(countries);
        }

        [Route("highscores")]
        public async Task<IActionResult> GetHighscores(int offset = 0, int limit = 50, string? order = "desc")
        {
            await using var db = new DatabaseContext();

            var query = db.Scores
                .AsNoTracking()
                .Where(x => x.UpdateTime > updateService.CalculationModuleUpdateTime && x.LocalPp > highscoreThreshold);

            var totalNotFilteredAmt = await query.CountAsync();

            if (!string.IsNullOrEmpty(order))
            {
                query = order == "asc" ? query.OrderBy(x=> x.LocalPp) : query.OrderByDescending(x => x.LocalPp);
            }

            // clamp limit
            if (limit > 500)
                limit = 500;

            var scores = await query.Skip(offset)
                .Take(limit)
                .Include(x => x.Map)
                .Include(x => x.Player)
                .Select(x=> new HighscoreModel
                {
                    Accuracy = x.Accuracy,
                    Combo = x.Combo,
                    LivePp = x.LivePp,
                    LocalPp = x.LocalPp,
                    Map = x.Map,
                    Misses = x.Misses,
                    Mods = x.Mods,
                    Player = x.Player
                })
                .ToArrayAsync();

            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i].Player != null)
                    scores[i].Player.Scores = null; // fixes recursive referencing

                scores[i].Position = i + offset + 1;
            }

            return new JsonResult(new PaginationModel<HighscoreModel>
            {
                Rows = scores.ToArray(),
                Total = totalNotFilteredAmt,
                TotalNotFiltered = totalNotFilteredAmt,
                Offset = offset
            });
        }

        private string FixupSort(string? sort)
        {
            // this is stupid
            return sort switch
            {
                "name" => "Name",
                "livePp" => "LivePp",
                "ppLoss" => "PpLoss",
                _ => "LocalPp"
            };
        }
    }
}
