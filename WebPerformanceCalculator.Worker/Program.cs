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
        private static readonly HttpClient http = new HttpClient();

#if DEBUG
        private const string remote_in_endpoint = @"http://localhost:6000/GetUserForWorker";
        private const string remote_out_endpoint = @"http://localhost:6000/SubmitWorkerResults";
#else
        private const string remote_in_endpoint = @"https://newpp.stanr.info/GetUserForWorker";
        private const string remote_out_endpoint = @"https://newpp.stanr.info/SubmitWorkerResults";
#endif
        private const string auth_key = "";
        private const string api_key = "";

#if DEBUG
        private const int pooling_rate = 1000; // 1 second
#else
        private const int pooling_rate = 5000; // 5 seconds
#endif

        private class JsonUser
        {
            public string user { get; set; }
        }

        static void Main(string[] args)
        {
            var assemblyFileInfo = new FileInfo(typeof(Program).Assembly.Location);
            workingDir = assemblyFileInfo.DirectoryName;

            Console.WriteLine("Started...");

            while (true)
            {
                try
                {
                    var json = http.GetStringAsync($"{remote_in_endpoint}?key={auth_key}").Result;
                    if (!string.IsNullOrEmpty(json))
                    {
                        CalcUser(JsonConvert.DeserializeObject<JsonUser>(json).user);
                    }
                    else
                        Thread.Sleep(pooling_rate);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Thread.Sleep(pooling_rate);
                }
            }
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
                            $"PerformanceCalculator.dll profile \"{jsonUsername}\" {api_key}",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit(180000); // 3 mins

                if (File.Exists($"{workingDir}/players/{jsonUsername}.json"))
                    result = File.ReadAllText($"{workingDir}/players/{jsonUsername}.json");
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
