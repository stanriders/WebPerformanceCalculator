using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPerformanceCalculator.DbConvert.New.Types
{
    public class Score
    {
        [Key]
        public long Id { get; set; }

        public double LocalPp { get; set; }

        public double? LivePp { get; set; }

        public string? Mods { get; set; }

        public double Accuracy { get; set; }

        public int Combo { get; set; }

        public int Misses { get; set; }

        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// Things like aimPP, tapPP, accPP
        /// </summary>
        public string? AdditionalPpData { get; set; }

        public int PositionChange { get; set; }

        [ForeignKey(nameof(Types.Player.Id))]
        public int PlayerId { get; set; }
        public Player? Player { get; set; }

        [ForeignKey(nameof(Types.Map.Id))]
        public int MapId { get; set; }
        public Map? Map { get; set; }
    }
}
