using System.ComponentModel.DataAnnotations;

namespace WebPerformanceCalculator.DbConvert.New.Types
{
    public class Map
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = "Unknown Map";

        public int MaxCombo { get; set; }
    }
}
