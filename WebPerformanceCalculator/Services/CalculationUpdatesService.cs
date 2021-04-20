using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WebPerformanceCalculator.Services
{
    public class CalculationUpdatesService : CalculationService
    {
        public string CommitHash { get; set; } = "unknown";

        public DateTime CalculationModuleUpdateTime { get; set; }

        private readonly string commitHashFilePath;
        private readonly string calcFilePath;
        private readonly string calcUpdateLink;

        public CalculationUpdatesService(IConfiguration _configuration) : base(_configuration)
        {
            commitHashFilePath = _configuration["CommitHashFileName"] ?? "commithash";
            calcFilePath = Path.Combine(workingDirectory, _configuration["CalculationModuleFileName"] ?? "osu.Game.Rulesets.Osu.dll");
            calcUpdateLink = _configuration["CalculationModuleUpdateLink"] ?? "http://localhost/osu.Game.Rulesets.Osu.dll";

            Update();
        }

        /// <summary>
        /// Updates calculation module update time and commit hash 
        /// </summary>
        public void Update()
        {
            if (File.Exists(commitHashFilePath))
                CommitHash = File.ReadAllText(commitHashFilePath);

            CalculationModuleUpdateTime = File.GetLastWriteTime(calcFilePath).ToUniversalTime();
        }

        /// <summary>
        /// Check if date is older than the calculation module
        /// </summary>
        public bool IsOutdated(DateTime date)
        {
            return date < CalculationModuleUpdateTime;
        }

        /// <summary>
        /// Check if a file is older than the calculation module
        /// </summary>
        public bool IsOutdatedFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return true;

            var fileUpdateTime = File.GetLastWriteTime(path).ToUniversalTime();
            return fileUpdateTime < CalculationModuleUpdateTime;
        }

        public string GetUpdateLink()
        {
            return calcUpdateLink;
        }
    }
}
