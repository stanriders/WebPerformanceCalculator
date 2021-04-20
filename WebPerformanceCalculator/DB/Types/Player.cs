using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebPerformanceCalculator.DB.Types
{
    public class Player
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = "Unknown Player";

        public double LivePp { get; set; }

        public double LocalPp { get; set; }

        public double PlaycountPp { get; set; }

        public string? Country { get; set; }

        public DateTime UpdateTime { get; set; }

        public List<Score>? Scores { get; set; }

    }
}
