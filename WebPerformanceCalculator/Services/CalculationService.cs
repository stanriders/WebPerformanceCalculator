
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebPerformanceCalculator.DB;
using WebPerformanceCalculator.DB.Types;
using WebPerformanceCalculator.Models;

namespace WebPerformanceCalculator.Services
{
    public class CalculationService : IHostedService, IDisposable
    {
        private readonly ILogger<CalculationService> logger;
        private readonly PlayerQueueService queue;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMapper mapper;
        private Timer? timer;

        private AccessToken? accessToken;

        private readonly string clientId;
        private readonly string clientSecret;

        private readonly string key;

        private const double sotarkses_adjustment = 0.7;
        private readonly int[] sotarkses = new[]
        {
            4452992, // sotarks
            3533958, // fieryrage
            7451883, // nevo
            2308676, // fatfan kolek
            2732340, // taeyang
            3723568, // reform
            5115995, // armin
            4446810, // bibbity bill
            4378277, // log off now
            3361661, // azu
            4589786, // dendyhere
            9426712, // browiec
            2185987, // emu1337
            7785535, // onlybiscuit
            5404892, // kowari
            5156153  // DeRandom Otaku
        };

        private const double lesser_sotarkses_adjustment = 0.85;
        private readonly int[] lesser_sotarkses = new[]
        {
            4830261, // seni
            2706438, // monstrata
            2506267, // snownino_
            2773526, // xexxar
            899031,  // lami
            4754771, // akitoshi
            3230571, // doormat
            3178411, // kencho
            3552948, // skyflame
            6174349, // Kuki1537
            6203841, // Kagetsu
            5504231  // Shmiklak
        };

        private const double other_adjustment = 1.05;

        public CalculationService(ILogger<CalculationService> _logger, 
            PlayerQueueService _queue, 
            IHttpClientFactory _httpClientFactory, 
            IMapper _mapper,
            IConfiguration _configuration)
        {
            logger = _logger;
            queue = _queue;
            httpClientFactory = _httpClientFactory;
            mapper = _mapper;

            clientId = _configuration["ApiV2Client"];
            clientSecret = _configuration["ApiV2Secret"];

            key = _configuration["ApiV1Key"];
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("CalculationService running.");

            timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            var player = queue.GetPlayerForCalculation();
            if (player is not null)
            {
                logger.LogInformation("Calculating {Player}...", player);

                accessToken = await RefreshToken();

                var client = httpClientFactory.CreateClient("OsuApi");
                client.DefaultRequestHeaders.Add(HttpRequestHeader.Authorization.ToString(), $"Bearer {accessToken?.Token}");

                try
                {
                    var playerData =
                        JsonConvert.DeserializeObject<ApiPlayerModel[]>(
                            await client.GetStringAsync($"/api/get_user?k={key}&u={player}&m=0"));

                    if (playerData is not null && playerData.Length > 0)
                    {
                        logger.LogInformation("Got player data!");
                        var playerScores =
                            JsonConvert.DeserializeObject<ApiScore[]>(
                                await client.GetStringAsync($"/api/v2/users/{playerData[0].Id}/scores/best?&limit=100&mode=osu"));

                        if (playerScores is not null && playerScores.Length > 0)
                        {
                            await AdjustScores(playerData[0], playerScores);
                            Thread.Sleep(350);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }

                queue.FinishedCalculation(player);

                logger.LogInformation("Finished calculating {Player}", player);
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("CalculationService is stopping.");

            timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
        public void Dispose()
        {
            timer?.Dispose();
        }

        private async Task<AccessToken?> RefreshToken()
        {
            if (accessToken is not null && !accessToken.Expired) 
                return accessToken;

            var client = httpClientFactory.CreateClient("OsuApi");

            var authRequest = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "client_credentials",
                scope = "public"
            };

            var request = await client.PostAsJsonAsync("oauth/token", authRequest);
            if (request.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<AccessToken>(await request.Content.ReadAsStringAsync());
            }

            return null;
        }

        private async Task AdjustScores(ApiPlayerModel player, ApiScore[] playerScores)
        {
            await using var db = new DatabaseContext();

            // FIXME: only remove outdated scores
            db.RemoveRange(await db.Scores.Where(x => x.PlayerId == player.Id).ToArrayAsync());

            var scoresToAdd = new List<Score>();
            var mapsToAdd = new List<Map>();

            foreach (var score in playerScores)
            {
                logger.LogInformation("Adjusting score {Score}", score.Id);

                var result = mapper.Map<Score>(score);
                result.UpdateTime = DateTime.UtcNow;

                if (IsFreedomDiveHdHr(score))
                {
                    if (result.LivePp != null)
                        result.LocalPp = result.LivePp.Value * 2.0;
                }
                else
                {
                    var map = await db.Maps.FirstOrDefaultAsync(x => x.Id == score.BeatmapShort.Id);
                    if (map is not null)
                    {
                        if (result.LivePp != null)
                            result.LocalPp = result.LivePp.Value * map.AdjustmentPercentage;
                    }
                    else
                    {
                        var adjustmentPercent = AdjustScore(score);

                        if (result.LivePp != null)
                            result.LocalPp = result.LivePp.Value * adjustmentPercent;

                        mapsToAdd.Add(new Map
                        {
                            AdjustmentPercentage = adjustmentPercent,
                            Id = score.BeatmapShort.Id,
                            Name =
                                $"{score.BeatmapSet.Artist} - {score.BeatmapSet.Title} [{score.BeatmapShort.Version}]"
                        });
                    }
                }

                scoresToAdd.Add(result);
            }

            var mappedPlayer = mapper.Map<Player>(player);
            mappedPlayer.UpdateTime = DateTime.UtcNow;

            var localOrdered = scoresToAdd.OrderByDescending(x => x.LocalPp).ToList();
            var liveOrdered = scoresToAdd.OrderByDescending(x => x.LivePp).ToList();

            foreach (var score in scoresToAdd)
                score.PositionChange = liveOrdered.IndexOf(score) - localOrdered.IndexOf(score);

            int index = 0;
            double totalLocalPp = localOrdered.Select(x => x.LocalPp).Sum(play => Math.Pow(0.95, index++) * play);
            double totalLivePp = player.Pp;

            index = 0;
            double nonBonusLivePp = liveOrdered.Select(x => x.LivePp ?? 0.0).Sum(play => Math.Pow(0.95, index++) * play);

            //todo: implement properly. this is pretty damn wrong.
            var playcountBonusPp = (totalLivePp - nonBonusLivePp);
            totalLocalPp += playcountBonusPp;

            mappedPlayer.LocalPp = totalLocalPp;

            var dbPlayer = await db.Players.FirstOrDefaultAsync(x => x.Id == player.Id);
            if (dbPlayer != null)
            {
                dbPlayer.LivePp = mappedPlayer.LivePp;
                dbPlayer.LocalPp = mappedPlayer.LocalPp;
                dbPlayer.PlaycountPp = mappedPlayer.PlaycountPp;
                dbPlayer.Name = mappedPlayer.Name;
                dbPlayer.Country = mappedPlayer.Country;
                dbPlayer.UpdateTime = DateTime.UtcNow;
            }
            else
            {
                await db.Players.AddAsync(mappedPlayer);
            }

            await db.AddRangeAsync(mapsToAdd);
            await db.SaveChangesAsync(); // we save maps first so that scores won't fail because of failed foreign key constraint

            await db.AddRangeAsync(scoresToAdd);
            await db.SaveChangesAsync();
        }
        
        private double AdjustScore(ApiScore score)
        {
            if (sotarkses.Contains((int)score.BeatmapShort.CreatorId))
                return sotarkses_adjustment;

            if (lesser_sotarkses.Contains((int)score.BeatmapShort.CreatorId))
                return lesser_sotarkses_adjustment;

            return other_adjustment;
        }

        private bool IsFreedomDiveHdHr(ApiScore score)
        {
            string[] fdfdMods = { "HD", "HR" };
            var intersectedMods = fdfdMods.Intersect(score.Mods);
            var isHDHR = intersectedMods.Count() == fdfdMods.Length;

            if (score.BeatmapShort.Id == 129891 && isHDHR)
                return true;

            return false;
        }

        private class AccessToken
        {
            private DateTime expireDate;
            [JsonProperty("expires_in")]
            public long ExpiresIn
            {
                get => expireDate.Ticks;
                set => expireDate = DateTime.Now.AddSeconds(value);
            }

            public bool Expired => expireDate < DateTime.Now;

            [JsonProperty("access_token")] 
            public string Token { get; set; } = null!;
        }
    }
}
