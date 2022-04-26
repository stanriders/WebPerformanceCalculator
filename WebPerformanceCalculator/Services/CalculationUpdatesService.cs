using System;

namespace WebPerformanceCalculator.Services
{
    public class CalculationUpdatesService
    {
        public string CommitHash { get; set; } = "94e892d";
        public DateTime CalculationModuleUpdateTime { get; set; } = new(2022, 04, 26, 16, 47, 27);

        /// <summary>
        /// Check if date is older than the calculation module
        /// </summary>
        public bool IsOutdated(DateTime date)
        {
            return date < CalculationModuleUpdateTime;
        }
    }
}
