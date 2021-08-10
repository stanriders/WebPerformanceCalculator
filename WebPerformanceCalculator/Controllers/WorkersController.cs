using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentry;
using WebPerformanceCalculator.Attributes;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.DB.Types;
using WebPerformanceCalculator.Models;
using WebPerformanceCalculator.Services;
using WebPerformanceCalculator.Shared.Types;

namespace WebPerformanceCalculator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkersController : ControllerBase
    {
        private readonly PlayerQueueService playerQueue;
        private readonly CalculationUpdatesService updateService;
        private readonly ILogger<WorkersController> logger;

        private readonly Regex scoreRegex = new("(\\d+) - (.+]) \\+?(.+)? \\((\\d+(?>\\.\\d+)?)%, (\\d+)\\/(\\d+)?x(?>, (\\d+) \\w+)?\\)");
        private readonly Regex livePpRegex = new("(\\d+.\\d+) \\(\\D+(\\d+.\\d+)");

        public WorkersController(PlayerQueueService _playerQueue, CalculationUpdatesService _updateService, ILogger<WorkersController> _logger)
        {
            playerQueue = _playerQueue;
            updateService = _updateService;
            logger = _logger;
        }

        [HttpPost]
        [RequiresKey]
        public async Task<IActionResult> PostResults([FromBody] CalculatedPlayerModel content, string key, string queueUsername)
        {
            SentrySdk.ConfigureScope(scope => { scope.Contexts["Payload"] = new {
                QueueUsername = queueUsername,
                PlayerId = content.UserID
            }; });

            await using var db = new DatabaseContext();

            try
            {
                var livePpRegexMatches = livePpRegex.Match(content.LivePP).Groups;
                var livePp = double.Parse(livePpRegexMatches[1].Value, CultureInfo.InvariantCulture);
                var playcountPp = double.Parse(livePpRegexMatches[2].Value, CultureInfo.InvariantCulture);
                var localPp = Convert.ToDouble(content.LocalPP.Split(' ')[0], CultureInfo.InvariantCulture);

                var dbPlayer = await db.Players.FirstOrDefaultAsync(x => x.Id == content.UserID);
                if (dbPlayer != null)
                {
                    dbPlayer.LivePp = livePp;
                    dbPlayer.LocalPp = localPp;
                    dbPlayer.PlaycountPp = playcountPp;
                    dbPlayer.Name = content.Username;
                    dbPlayer.Country = content.UserCountry;
                    dbPlayer.UpdateTime = DateTime.UtcNow;
                }
                else
                {
                    await db.Players.AddAsync(new Player
                    {
                        Id = content.UserID,
                        LivePp = livePp,
                        LocalPp = localPp,
                        PlaycountPp = playcountPp,
                        Name = content.Username,
                        Country = content.UserCountry,
                        UpdateTime = DateTime.UtcNow
                    });
                }

                // FIXME: only remove outdated scores
                db.RemoveRange(await db.Scores.Where(x => x.PlayerId == content.UserID).ToArrayAsync());

                var scoresToAdd = new List<Score>();
                var mapsToAdd = new List<Map>();
                foreach (var score in content.Beatmaps)
                {
                    var processedScore = await ConvertLegacyScore(score, content.UserID);
                    if (processedScore != null)
                    {
                        scoresToAdd.Add(processedScore);
                        if (processedScore.Map != null)
                            mapsToAdd.Add(processedScore.Map);
                    }
                }

                await db.AddRangeAsync(mapsToAdd);
                await db.SaveChangesAsync(); // we save maps first so that scores won't fail because of failed foreign key constraint

                await db.AddRangeAsync(scoresToAdd);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogWarning(e.ToString());
            }
            finally
            {
                playerQueue.FinishedCalculation(queueUsername);
            }

            return Ok();
        }

        [HttpGet]
        [RequiresKey]
        public IActionResult GetPayload(string key, long calcTimestamp)
        {
            if (calcTimestamp == 0)
                return StatusCode(422);

            if (calcTimestamp < updateService.CalculationModuleUpdateTime.Ticks)
            {
                return new JsonResult(new WorkerDataModel
                {
                    Data = updateService.GetUpdateLink(),
                    DataType = DataType.Update
                });
            }

            var player = playerQueue.GetPlayerForCalculation();
            if (player != null)
            {
                return new JsonResult(new WorkerDataModel
                {
                    Data = player,
                    DataType = DataType.Profile
                });
            }

            return Ok();
        }

        /// <summary>
        /// Converts legacy score data into a new structure
        /// </summary>
        /// <param name="score">Legacy score</param>
        /// <param name="playerId">Player ID</param>
        /// <returns>Score</returns>
        private async Task<Score?> ConvertLegacyScore(CalculatedPlayerModel.CalculatedPlayerBeatmap score, int playerId)
        {
            await using var db = new DatabaseContext();

            var regexMatches = scoreRegex.Match(score.Beatmap);
            if (!regexMatches.Success)
                return null;

            var mapId = int.Parse(regexMatches.Groups[1].Value);
            var mapName = regexMatches.Groups[2].Value;
            var mods = regexMatches.Groups[3].Value;
            var accuracy = double.Parse(regexMatches.Groups[4].Value);
            var combo = int.Parse(regexMatches.Groups[5].Value);

            var maxCombo = 0;
            if (regexMatches.Groups[6].Success)
                maxCombo = int.Parse(regexMatches.Groups[6].Value);

            var misses = 0;
            if (regexMatches.Groups[7].Success)
                misses = int.Parse(regexMatches.Groups[7].Value);

            if (score.PositionChange == "-")
                score.PositionChange = "0";

            Map? map = null;
            var dbMap = await db.Maps.FirstOrDefaultAsync(x => x.Id == mapId);
            if (dbMap == null)
            {
                map = new Map
                {
                    Id = mapId,
                    Name = mapName,
                    MaxCombo = maxCombo
                };
            }
            else if (dbMap.MaxCombo != maxCombo || dbMap.Name != mapName)
            {
                dbMap.MaxCombo = maxCombo;
                dbMap.Name = mapName;
            }

            return new Score
            {
                Id = score.Id ?? 0,
                LocalPp = double.Parse(score.LocalPP),
                LivePp = double.Parse(score.LivePP),
                PlayerId = playerId,
                UpdateTime = DateTime.UtcNow,
                MapId = mapId,
                Map = map,
                Mods = mods,
                Accuracy = accuracy,
                Combo = combo,
                Misses = misses,
                AdditionalPpData = $"aimPP = {score.AimPP}, tapPP = {score.TapPP}, accPP = {score.AccPP}",
                PositionChange = int.Parse(score.PositionChange)
            };
        }
    }
}
