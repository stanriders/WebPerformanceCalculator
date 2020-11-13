
using System;

namespace FullTopBuilder
{
    public class PlayerModel
    {
        public string Username { get; set; }
        public string LivePP { get; set; }
        public string LocalPP { get; set; }
        public double LocalPPNumeric { get; set; }
        public string SitePP { get; set; }
        public double? SitePPNumeric { get; set; }
        public string PPChange { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool Outdated { get; set; }
        public string UserCountry { get; set; }

        public override string ToString() => $"{Username} - {LocalPPNumeric}";
    }
}
