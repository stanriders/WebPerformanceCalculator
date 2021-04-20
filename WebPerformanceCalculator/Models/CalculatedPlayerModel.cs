
using System.Collections.Generic;

namespace WebPerformanceCalculator.Models
{
    public class CalculatedPlayerModel
    {
        #nullable disable
        public class CalculatedPlayerBeatmap
        {
            public long? Id { get; set; }
            public string Beatmap { get; set; }
            public string LivePP { get; set; }
            public string LocalPP { get; set; }
            public string ComparePP { get; set; }
            public string PPChange { get; set; }
            public string PositionChange { get; set; }
            public string AimPP { get; set; }
            public string TapPP { get; set; }
            public string AccPP { get; set; }
        }

        public int UserID { get; set; }
        public string Username { get; set; }
        public string UserCountry { get; set; }
        public string LivePP { get; set; }
        public string LocalPP { get; set; }
        public string SitePP { get; set; }
        public List<CalculatedPlayerBeatmap> Beatmaps { get; set; } = new();
    }
}
