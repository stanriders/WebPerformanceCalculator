using System.ComponentModel.DataAnnotations;

namespace WebPerformanceCalculator.DB.Types
{
    public class Map
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = "Unknown Map";

        public int MaxCombo { get; set; }

        public double AdjustmentPercentage { get; set; }
    }
}
