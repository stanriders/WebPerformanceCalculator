using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPerformanceCalculator.Shared.Types;

namespace WebPerformanceCalculator.Worker
{
    public static class Program
    {
        private static HttpClient http;

        private static string workingDirectory;
        private static string calculatorPath;
        private static string calculatorModulePath;
        private static string apiKey;

        private static string getWorkEndpoint;
        private static string submitWorkEndpoint;
        private static int poolingRate;
        private static string endpointKey;

        private const string lock_file = "lockcalc";
        private const int process_wait_time = 3 * 60 * 1000;

        static void Main(string[] args)
        {
            if (!Configure())
                return;

            http = new HttpClient();
            Log("Started...");

            while (!File.Exists("softexit"))
            {
                try
                {
                    if (!File.Exists(lock_file))
                    {
                        Loop();
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
                Thread.Sleep(poolingRate);
            }

            http.Dispose();
            Log("Exiting...");
        }

        private static void Loop()
        {
            var calcDate = File.GetLastWriteTime(calculatorModulePath).ToUniversalTime();
            var json = http.GetStringAsync($"{getWorkEndpoint}?key={endpointKey}&calcTimestamp={calcDate.Ticks}").Result;
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonConvert.DeserializeObject<WorkerDataModel>(json);
                switch (data.DataType)
                {
                    case DataType.Profile:
                    {
                        CalcUser(data.Data);
                        break;
                    }
                    case DataType.Update:
                    {
                        UpdateCalc(data.Data);
                        break;
                    }
                }
            }
        }

        private static void UpdateCalc(string calcLink)
        {
            Log($"OUTDATED CALC MODULE ({File.GetLastWriteTime(calculatorModulePath).ToUniversalTime()})! Updating...");
            try
            {
                Log("Locking calculation...");
                var lockFile = File.Create(lock_file);
                lockFile.Dispose();

                Thread.Sleep(10000);

                Log("Downloading new calc module...");
                var calcBytes = http.GetByteArrayAsync(calcLink).Result;
                File.WriteAllBytes(calculatorModulePath, calcBytes);

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
                var queueUsername = username.Replace(' ', '_');

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        WorkingDirectory = workingDirectory,
                        Arguments =
                            $"{calculatorPath} profile \"{queueUsername}\" {apiKey}",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit(process_wait_time); // 3 mins

                if (File.Exists($"{workingDirectory}/players/{queueUsername}.json"))
                {
                    result = File.ReadAllText($"{workingDirectory}/players/{queueUsername}.json");
                    File.Delete($"{workingDirectory}/players/{queueUsername}.json");
                }
            }
            catch (Exception e)
            {
                Log($"Failed to calc {username}\n {e.Message}");
            }
            Log($"Sending {username} results...");

            var content = new StringContent(result, Encoding.UTF8, "application/json");
            var response = http.PostAsync($"{submitWorkEndpoint}?key={endpointKey}&queueUsername={username}", content).Result;
            if (response.IsSuccessStatusCode)
            {
                Log("Sent!");
            }
            else
            {
                Log($"Failed! {response.StatusCode} - {response.ReasonPhrase}");
            }
        }

        private static void Log(string log)
        {
            Console.WriteLine($"[{DateTime.Now}] {log}");
        }

        private static bool Configure()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            workingDirectory = configuration["CalculatorPath"];
            if (string.IsNullOrEmpty(workingDirectory))
                workingDirectory = new FileInfo(typeof(Program).Assembly.Location).DirectoryName ?? ".";

            calculatorPath = Path.Combine(workingDirectory, "PerformanceCalculator.dll");
            calculatorModulePath = Path.Combine(workingDirectory, configuration["CalculationModuleFileName"] ?? "osu.Game.Rulesets.Osu.dll");

            apiKey = configuration["APIKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Log("API Key is empty!");
                return false;
            }

            getWorkEndpoint = configuration["GetWorkEndpointAddress"];
            submitWorkEndpoint = configuration["SubmitWorkEndpointAddress"];
            if (string.IsNullOrEmpty(getWorkEndpoint) || string.IsNullOrEmpty(submitWorkEndpoint))
            {
                Log("Work endpoint addresses are empty!");
                return false;
            }

            if (!int.TryParse(configuration["PollingRate"], out poolingRate))
                poolingRate = 5000;

            endpointKey = configuration["Key"];

            return true;
        }
    }
}
