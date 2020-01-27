using System;

namespace WebPerformanceCalculator.DB
{
    public class Score : IEquatable<Score>
    {
        public double PP { get; set; }

        public double? LivePP { get; set; }

        public string Player { get; set; }

        public string JsonName { get; set; }

        public string Map { get; set; }

        public DateTime CalcTime { get; set; }

        public bool Equals(Score other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PP.Equals(other.PP) && Player == other.Player && Map == other.Map;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Score) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PP.GetHashCode();
                hashCode = (hashCode * 397) ^ (Player != null ? Player.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Map != null ? Map.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
