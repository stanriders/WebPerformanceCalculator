using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebPerformanceCalculator
{
    public static class CalcQueue
    {
        private static readonly List<string> processingList = new List<string>();
        private static readonly Queue<string> usernameQueue = new Queue<string>();
        private static string workingDir;
        private static object dequeueLock = new object();

        public static bool AddToQueue(string username)
        {
            if (string.IsNullOrEmpty(workingDir))
            {
                var assemblyFileInfo = new FileInfo(typeof(CalcQueue).Assembly.Location);
                workingDir = assemblyFileInfo.DirectoryName;
            }

            username = username.ToLowerInvariant();
            if (usernameQueue.All(x => x != username) && CheckUserCalcDate(username) )
            {
                usernameQueue.Enqueue(username);
                return true;
            }

            return false;
        }

        public static string[] GetQueue()
        {
            var queue = new List<string>(processingList);
            queue.AddRange(usernameQueue);
            return queue.ToArray();
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
            lock (dequeueLock)
            {
                if (usernameQueue.Count > 0)
                {
                    var user = usernameQueue.Dequeue();
                    processingList.Add(user);
                    return user;
                }

                return string.Empty;
            }
        }

        public static void RemoveFromProcessing(string username)
        {
            processingList.RemoveAt(processingList.IndexOf(username));
        }
    }
}
