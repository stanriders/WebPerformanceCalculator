using System;
using System.ComponentModel.DataAnnotations;

namespace WebPerformanceCalculator.DbConvert.Old.Types
{
#nullable disable
    public class Player
    {
        [Key]
        public long Id { get; set; }

        public string JsonName { get; set; }

        public string Name { get; set; }

        public double LivePp { get; set; }

        public double LocalPp { get; set; }

        public double PpLoss { get; set; }

        public string Country { get; set; }

        //public DateTime UpdateTime { get; set; }
    }
}
