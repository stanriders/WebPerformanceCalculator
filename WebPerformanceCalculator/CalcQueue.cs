using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebPerformanceCalculator
{
    public static class CalcQueue
    {
        //private static readonly List<string> processingList = new List<string>();
        private static readonly ConcurrentQueue<string> usernameQueue = new ConcurrentQueue<string>();
        private static string workingDir;
        private static readonly object processingLock = new object();

        public static bool AddToQueue(string username)
        {
            if (string.IsNullOrEmpty(workingDir))
            {
                var assemblyFileInfo = new FileInfo(typeof(CalcQueue).Assembly.Location);
                workingDir = assemblyFileInfo.DirectoryName;
            }

            username = username.ToLowerInvariant();
            if (!usernameQueue.Contains(username) && CheckUserCalcDate(username))
            {
                usernameQueue.Enqueue(username);
                return true;
            }

            return false;
        }

        public static string[] GetQueue()
        {
            //var queue = new List<string>(processingList);
            //queue.AddRange(usernameQueue);
            return usernameQueue.ToArray();
        }

        private static bool CheckUserCalcDate(string jsonUsername)
        {
            if (!File.Exists($"{workingDir}/players/{jsonUsername}.json"))
                return true;

            var userCalcDate = File.GetLastWriteTime($"{workingDir}/players/{jsonUsername}.json").ToUniversalTime();
            var calcUpdateDate = File.GetLastWriteTime($"{workingDir}/osu.Game.Rulesets.Osu.dll").ToUniversalTime();
            if (userCalcDate < calcUpdateDate)
                return true;

            return false;
        }

        public static string GetUserForCalc()
        {
            if (usernameQueue.TryDequeue(out var user))
            {
                //processingList.Add(user);
                return user;
            }

            return string.Empty;
        }

        public static void RemoveFromProcessing(string username)
        {
            /*lock (processingLock)
            {
                processingList.RemoveAt(processingList.IndexOf(username));
            }*/
        }
    }
}
