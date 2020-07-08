
namespace WebPerformanceCalculator.DB
{
    public class PlayerSearchQuery
    {
        public int Rank { get; set; }
        public int LiveRank { get; set; }
        public long ID { get; set; }
        public string Name { get; set; }
        public string JsonName { get; set; }
        public double LivePP { get; set; }
        public double LocalPP { get; set; }
        public double PPLoss { get; set; }
        public string Country { get; set; }
    }
}
