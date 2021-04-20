
using WebPerformanceCalculator.DB.Types;

namespace WebPerformanceCalculator.Models
{
    public class TopPlayerModel : Player
    {
        public int LivePlace { get; set; }
        public int Place { get; set; }
        public int RankChange { get; set; }
    }
}
