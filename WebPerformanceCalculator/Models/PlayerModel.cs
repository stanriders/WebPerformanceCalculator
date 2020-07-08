using System;

namespace WebPerformanceCalculator.Models
{
    public class PlayerModel
    {
        public class ResultBeatmap
        {
            public string Beatmap { get; set; }
            public string LivePP { get; set; }
            public string LocalPP { get; set; }
            public string ComparePP { get; set; }
            public string PPChange { get; set; }
            public string PositionChange { get; set; }
            public string AimPP { get; set; }
            public string TapPP { get; set; }
            public string AccPP { get; set; }
            public string ReadingPP { get; set; }
        }

        public int UserID { get; set; }
        public string Username { get; set; }
        public string UserCountry { get; set; }
        public string LivePP { get; set; }
        public string LocalPP { get; set; }
        public string SitePP { get; set; }
        public ResultBeatmap[] Beatmaps { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
