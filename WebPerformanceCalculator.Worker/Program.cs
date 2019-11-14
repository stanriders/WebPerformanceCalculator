using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using WebPerformanceCalculator.Shared;

namespace WebPerformanceCalculator.Worker
{
    class Program
    {
        private static string workingDir;
        private static string apiKey;
        private static readonly HttpClient http = new HttpClient();

#if DEBUG
        private const string remote_in_endpoint = @"http://localhost:6000/api/GetUserForWorker";
        private const string remote_out_endpoint = @"http://localhost:6000/api/SubmitWorkerResults";
        private const int pooling_rate = 1000; // 1 second
#else
        private const string remote_in_endpoint = @"https://newpp.stanr.info/api/GetUserForWorker";
        private const string remote_out_endpoint = @"https://newpp.stanr.info/api/SubmitWorkerResults";
        private const int pooling_rate = 5000; // 5 seconds
#endif

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
                Log("API Key is empty! Using fallback key...");
                apiKey = fallback_api_key;
            }

            Log("Started...");

            while (!File.Exists("softexit"))
            {
                try
                {
                    if (!File.Exists(lock_file))
                    {
                        var calcDate = File.GetLastWriteTime(calc_file).ToUniversalTime();
                        var json = http.GetStringAsync($"{remote_in_endpoint}?key={Config.auth_key}&calcTimestamp={calcDate.Ticks}").Result;
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
                        Log("Calculation is locked.");
                    }
                }
                catch (Exception e)
                {
                    Log(e.Message);
                }
                Thread.Sleep(pooling_rate);
            }
            Log("Exiting...");
        }

        private static void UpdateCalc(string calcLink)
        {
            Log("OUTDATED CALC MODULE! Updating...");
            try
            {
                Log("Locking calculation...");
                var lockFile = File.Create(lock_file);
                lockFile.Dispose();

                Thread.Sleep(10000);

                Log("Downloading new calc module...");
                var calcBytes = http.GetByteArrayAsync(calcLink).Result;
                File.WriteAllBytes(calc_file, calcBytes);

                Log("Unlocking calculation...");
                File.Delete(lock_file);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
            Log("Finished updating");
        }

        private static void CalcUser(string username)
        {
            var result = string.Empty;
            try
            {
                Log($"Calculating {username}");
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
                Log($"Failed to calc {username}\n {e.Message}");
            }
            Log($"Sending {username} results");

            var content = new StringContent(result, Encoding.UTF8, "application/json");
            http.PostAsync($"{remote_out_endpoint}?key={Config.auth_key}&jsonUsername={username}", content).Wait();
        }

        private static void Log(string log)
        {
            Console.WriteLine($"[{DateTime.Now}] {log}");
        }
    }
}
