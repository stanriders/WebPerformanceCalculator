using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebPerformanceCalculator.Controllers;
using WebPerformanceCalculator.DB;

namespace WebPerformanceCalculator
{
    public static class CalcQueue
    {
        private static readonly MemoryCache cache = new MemoryCache("usernames");
        private static readonly Queue<string> usernameQueue = new Queue<string>();
        private static bool isBusy = false;
        private static DateTime queueDebounce = DateTime.Now;

        private const string api_key = "";

        public static bool AddToQueue(string username)
        {
            if (usernameQueue.All(x => x != username) && !cache.Contains(username) && queueDebounce < DateTime.Now)
            {
                if (!isBusy && usernameQueue.Count <= 0)
                    CalcUser(username);

                usernameQueue.Enqueue(username);
                queueDebounce = DateTime.Now.AddSeconds(1);
                return true;
            }

            return false;
        }

        public static string[] GetQueued()
        {
            return usernameQueue.ToArray();
        }

        private static void CalcUser(string username)
        {
            Task.Run(() =>
            {
                isBusy = true;

                Thread.CurrentThread.Name = $"Calc {username}";
                try
                {
                    var assemblyFileInfo = new FileInfo(typeof(HomeController).Assembly.Location);
                    var workingDir = assemblyFileInfo.DirectoryName;
                    var jsonUsername = username.ToLowerInvariant().Replace(' ', '_');

                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            WorkingDirectory = workingDir,
                            Arguments =
                                $"PerformanceCalculator.dll profile {jsonUsername} {api_key}",
                            RedirectStandardOutput = false,
                            UseShellExecute = true,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    process.WaitForExit();

                    var result = string.Empty;
                    if (File.Exists($"{workingDir}/{jsonUsername}.json"))
                    {
                        result = File.ReadAllText($"{workingDir}/{jsonUsername}.json");

                        var json = JObject.Parse(result);

                        using (DatabaseContext db = new DatabaseContext())
                        {
                            var userid = Convert.ToInt64(json["UserID"].ToString().Split(' ')[0]);
                            var osuUsername = json["Username"].ToString();
                            var livePP = Convert.ToDouble(json["LivePP"].ToString().Split(' ')[0],
                                System.Globalization.CultureInfo.InvariantCulture);
                            var localPP = Convert.ToDouble(json["LocalPP"].ToString().Split(' ')[0],
                                System.Globalization.CultureInfo.InvariantCulture);
                            if (db.Players.Any(x => x.ID == userid))
                            {
                                var player = db.Players.Single(x => x.ID == userid);
                                player.LivePP = livePP;
                                player.LocalPP = localPP;
                                player.PPLoss = localPP - livePP;
                            }
                            else
                            {
                                db.Players.Add(new Player()
                                {
                                    ID = userid,
                                    LivePP = livePP,
                                    LocalPP = localPP,
                                    PPLoss = localPP - livePP,
                                    Name = osuUsername,
                                    JsonName = jsonUsername
                                });
                            }

                            db.SaveChanges();
                        }

                        cache.Add(username, username, DateTimeOffset.Now.AddHours(1));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to calc {username}\n {e.Message}");
                }
                isBusy = false;

                // remove ourself
                usernameQueue.Dequeue();

                if (usernameQueue.Count > 0)
                    CalcUser(usernameQueue.Peek()); // calc next user but keep them in queue
            });
        }
    }
}
