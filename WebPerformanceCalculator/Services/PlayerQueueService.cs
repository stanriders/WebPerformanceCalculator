using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Configuration;

namespace WebPerformanceCalculator.Services
{
    public class PlayerQueueService : IDisposable
    {
        private MemoryCache calculatedCache = new("calculated_players"); // cache for recently calculated players
        private readonly double allowRecalcAfter; // how often player can be recalculated, in hours

        private readonly ConcurrentQueue<string> playerQueue = new(); // main calculation queue
        private DateTime queueDebounce = DateTime.Now;
        private bool queueLocked;

        private MemoryCache calculatingCache = new("currently_calculating"); // cache for currently calculating players
        private const int drop_calculation_after = 5; // minutes

        private readonly MemoryCache usersCache = new("users"); // cache for recently calculated players
        private readonly double calcsPerHour; // how many profiles one user can calc in an hour

        private static readonly Regex username_regex = new(@"^[A-Za-z0-9-\[\]_ ]+$", RegexOptions.Compiled);

        public PlayerQueueService(IConfiguration _configuration)
        {
            if (!double.TryParse(_configuration["CalcsPerHourPerUser"], out calcsPerHour))
                calcsPerHour = 15.0;

            if (!double.TryParse(_configuration["AllowPlayerRecalcAfterHours"], out allowRecalcAfter))
                allowRecalcAfter = 24.0;
        }

        /// <summary>
        /// Runs validations and then adds player to the calculation queue
        /// </summary>
        /// <param name="player">Player name</param>
        /// <param name="remoteIp">IP of the user that added player to the queue</param>
        /// <returns>(success, error message)</returns>
        public string AddToQueue(string player, string? remoteIp)
        {
            if (queueLocked)
                return "Queue is temporary locked, try again later!";

            if (queueDebounce > DateTime.Now)
                return "Try again later";

            // performance calculator doesn't want to work with them even when escaping
            if (player.Contains('-'))
                return "Please use user ID instead";

            if (player.Length < 3 || 
                player.Length > 16 ||
                !username_regex.IsMatch(player) || 
                player.Contains("_full"))
                return "Incorrect username";

            var userCalcs = 0;
            if (remoteIp != null && usersCache.Contains(remoteIp))
            {
                userCalcs = (int)usersCache[remoteIp];
            }

            if (userCalcs > calcsPerHour)
                return "Chill";

            player = HttpUtility.HtmlEncode(player.Trim()).ToLowerInvariant();

            if (playerQueue.Contains(player) || calculatedCache.Contains(player) || calculatingCache.Contains(player))
                return $"This player doesn't need a recalculation yet! You can only recalculate once every {allowRecalcAfter} hours";

            playerQueue.Enqueue(player);
            queueDebounce = DateTime.Now.AddSeconds(1);

            userCalcs++;
            usersCache.Set(remoteIp, userCalcs, DateTimeOffset.Now.AddHours(userCalcs / calcsPerHour));
            return string.Empty;
        }

        /// <summary>
        /// Adds player to the calculation queue, only meant to be used from inside of the app
        /// </summary>
        /// <param name="player">Player name</param>
        public void AddToQueueInternal(string player)
        {
            if (player.Length < 3 ||
                player.Length > 16 ||
                !username_regex.IsMatch(player) ||
                player.Contains("_full"))
                return;

            player = HttpUtility.HtmlEncode(player.Trim()).ToLowerInvariant();
            playerQueue.Enqueue(player);
        }

        /// <summary>
        /// Returns all players that are going to be calculated
        /// </summary>
        /// <returns>An array of player names</returns>
        public string[] GetQueue()
        {
            return playerQueue.ToArray();
        }

        /// <summary>
        /// Returns all players that are being calculated right now
        /// </summary>
        /// <returns>An array of player names</returns>
        public string[] GetCalculatingPlayers()
        {
            return calculatingCache.Select(x=> x.Key).ToArray();
        }

        /// <summary>
        /// Returns users that recently added players to the queue
        /// </summary>
        /// <returns>Dictionary of IPs and amount of players they added</returns>
        public Dictionary<string,int> GetUsersStats()
        {
            return usersCache.ToDictionary(x=> x.Key, y => (int)y.Value);
        }

        /// <summary>
        /// Toggles adding new players to the queue
        /// </summary>
        public void ToggleQueue()
        {
            queueLocked = !queueLocked;
        }

        /// <summary>
        /// Dequeues a player for calculation
        /// </summary>
        /// <returns>Player name</returns>
        public string? GetPlayerForCalculation()
        {
            if (playerQueue.TryDequeue(out var player))
            {
                calculatingCache.Add(player, player, DateTimeOffset.Now.AddMinutes(drop_calculation_after));

                return player;
            }

            return null;
        }

        /// <summary>
        /// Removes player from currently calculating list and adds a cooldown
        /// </summary>
        /// <param name="player">Player name</param>
        public void FinishedCalculation(string player)
        {
            calculatingCache.Remove(player);

            calculatedCache.Add(player, player, DateTimeOffset.Now.AddHours(allowRecalcAfter));
        }

        /// <summary>
        /// Clears queue
        /// </summary>
        public void ClearQueue()
        {
            playerQueue.Clear();
        }

        /// <summary>
        /// Clears cooldown and currently calculating caches
        /// </summary>
        public void ClearCaches()
        {
            calculatedCache.Dispose();
            calculatedCache = new MemoryCache("calculated_players");

            calculatingCache.Dispose();
            calculatingCache = new MemoryCache("currently_calculating");
        }

        public void Dispose()
        {
            calculatedCache.Dispose();
            calculatingCache.Dispose();
            usersCache.Dispose();
        }
    }
}
