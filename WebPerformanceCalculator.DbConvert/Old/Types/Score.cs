using System;

namespace WebPerformanceCalculator.DbConvert.Old.Types
{
#nullable disable
    public class Score
    {
        public double PP { get; set; }

        public double? LivePp { get; set; }

        public string Player { get; set; }

        public string JsonName { get; set; }

        public string Map { get; set; }

        public DateTime CalcTime { get; set; }
    }
}
