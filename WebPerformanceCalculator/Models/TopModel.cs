
using WebPerformanceCalculator.DB;

namespace WebPerformanceCalculator.Models
{
    public class TopPlayerModel : Player
    {
        public int Place { get; set; }
    }

    public class TopModel
    {
        public int Total { get; set; }

        public int TotalNotFiltered { get; set; }

        public TopPlayerModel[] Rows { get; set; }
    }
}
