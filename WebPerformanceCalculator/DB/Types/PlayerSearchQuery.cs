
using Microsoft.EntityFrameworkCore;

namespace WebPerformanceCalculator.DB.Types
{
    [Keyless]
    public class PlayerSearchQuery : Player
    {
        public int Rank { get; set; }

        public int LiveRank { get; set; }
    }
}
