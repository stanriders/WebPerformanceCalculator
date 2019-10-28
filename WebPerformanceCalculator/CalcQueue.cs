using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebPerformanceCalculator.Controllers;
using WebPerformanceCalculator.DB;

namespace WebPerformanceCalculator
{
    public static class CalcQueue
    {
        private static readonly Queue<string> usernameQueue = new Queue<string>();
        private static bool isBusy = false;

        private static string workingDir;

        private const string api_key = "";

        public static bool AddToQueue(string username)
        {
            if (string.IsNullOrEmpty(workingDir))
            {
                var assemblyFileInfo = new FileInfo(typeof(HomeController).Assembly.Location);
                workingDir = assemblyFileInfo.DirectoryName;
            }

            username = username.ToLowerInvariant();
            if (usernameQueue.All(x => x != username) && CheckUserCalcDate(username) )
            {
                if (!isBusy && usernameQueue.Count <= 0)
                    CalcUser(username);

                usernameQueue.Enqueue(username);
                return true;
            }

            return false;
        }

        public static string[] GetQueued()
        {
            return usernameQueue.ToArray();
        }

        private static bool CheckUserCalcDate(string jsonUsername)
        {
            if (!File.Exists($"{workingDir}/{jsonUsername}.json"))
                return true;

            var userCalcDate = File.GetLastWriteTime($"{workingDir}/players/{jsonUsername}.json").ToUniversalTime();
            var calcUpdateDate = File.GetLastWriteTime($"{workingDir}/osu.Game.Rulesets.Osu.dll").ToUniversalTime();
            if (userCalcDate < calcUpdateDate)
                return true;

            return false;
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
                                $"PerformanceCalculator.dll profile \"{jsonUsername}\" {api_key}",
                            RedirectStandardOutput = false,
                            UseShellExecute = true,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    process.WaitForExit(300000); // 5 mins

                    var result = string.Empty;
                    if (File.Exists($"{workingDir}/players/{jsonUsername}.json"))
                    {
                        result = File.ReadAllText($"{workingDir}/players/{jsonUsername}.json");

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
