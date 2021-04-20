using System;

namespace WebPerformanceCalculator.Models
{
    public class CalculateMapModel
    {
        public string? Map { get; set; }
        public string[] Mods { get; set; } = Array.Empty<string>();
    }
}
