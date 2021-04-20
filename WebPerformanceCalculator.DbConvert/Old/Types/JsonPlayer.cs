using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebPerformanceCalculator.DbConvert.Old.Types
{
#nullable disable
    public class JsonPlayer
    {
        public class ResultBeatmap
        {
            public string Beatmap { get; set; }
            public string LivePP { get; set; }
            public string LocalPP { get; set; }
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
        public ResultBeatmap[] Beatmaps { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
