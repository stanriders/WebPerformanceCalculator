using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace WebPerformanceCalculator.Worker
{
    class Program
    {
        private static string workingDir;
        private static string apiKey;
        private static readonly HttpClient http = new HttpClient();

#if DEBUG
        private const string remote_in_endpoint = @"http://localhost:6000/GetUserForWorker";
        private const string remote_out_endpoint = @"http://localhost:6000/SubmitWorkerResults";
        private const int pooling_rate = 1000; // 1 second
#else
        private const string remote_in_endpoint = @"https://newpp.stanr.info/GetUserForWorker";
        private const string remote_out_endpoint = @"https://newpp.stanr.info/SubmitWorkerResults";
        private const int pooling_rate = 5000; // 5 seconds
#endif

        private const string auth_key = "";
        private const string fallback_api_key = "";
        private const string lock_file = "lockcalc";
        private const string calc_file = "osu.Game.Rulesets.Osu.dll";

        static void Main(string[] args)
        {
            var assemblyFileInfo = new FileInfo(typeof(Program).Assembly.Location);
            workingDir = assemblyFileInfo.DirectoryName;

            apiKey = File.ReadAllText("apikey");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API Key is empty! Using fallback key...");
                apiKey = fallback_api_key;
            }

            Console.WriteLine("Started...");

            while (!File.Exists("softexit"))
            {
                try
                {
                    if (!File.Exists(lock_file))
                    {
                        var calcDate = File.GetLastWriteTime(calc_file).ToUniversalTime();
                        var json = http.GetStringAsync($"{remote_in_endpoint}?key={auth_key}&calcTimestamp={calcDate.Ticks}").Result;
                        if (!string.IsNullOrEmpty(json))
                        {
                            var data = JsonConvert.DeserializeObject<WorkerDataModel>(json);
                            if (data.NeedsCalcUpdate)
                                UpdateCalc(data.Data);
                            else
                                CalcUser(data.Data);

                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Calculation is locked.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Thread.Sleep(pooling_rate);
            }
            Console.WriteLine("Exiting...");
        }

        private static void UpdateCalc(string calcLink)
        {
            Console.WriteLine("OUTDATED CALC MODULE! Updating...");
            try
            {
                Console.WriteLine("Locking calculation...");
                var lockFile = File.Create(lock_file);
                lockFile.Dispose();

                Thread.Sleep(10000);

                Console.WriteLine("Downloading new calc module...");
                var calcBytes = http.GetByteArrayAsync(calcLink).Result;
                File.WriteAllBytes(calc_file, calcBytes);

                Console.WriteLine("Unlocking calculation...");
                File.Delete(lock_file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Finished updating");
        }

        private static void CalcUser(string username)
        {
            var result = string.Empty;
            try
            {
                Console.WriteLine($"Calculating {username}");
                var jsonUsername = username.Replace(' ', '_');

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        WorkingDirectory = workingDir,
                        Arguments =
                            $"PerformanceCalculator.dll profile \"{jsonUsername}\" {apiKey}",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit(180000); // 3 mins

                if (File.Exists($"players/{jsonUsername}.json"))
                    result = File.ReadAllText($"players/{jsonUsername}.json");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to calc {username}\n {e.Message}");
            }
            Console.WriteLine($"Sending {username} results");

            var content = new StringContent(result, Encoding.UTF8, "application/json");
            http.PostAsync($"{remote_out_endpoint}?key={auth_key}&jsonUsername={username}", content).Wait();
        }
    }
}
