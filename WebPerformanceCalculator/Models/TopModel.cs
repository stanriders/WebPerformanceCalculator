
using WebPerformanceCalculator.DB;

namespace WebPerformanceCalculator.Models
{
    public class TopPlayerModel : Player
    {
        public int LivePlace { get; set; }
        public int Place { get; set; }
        public int RankChange { get; set; }
    }

    public class TopModel
    {
        public int Total { get; set; }

        public int TotalNotFiltered { get; set; }

        public TopPlayerModel[] Rows { get; set; }
    }
}
