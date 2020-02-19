
using System.Text.Json.Serialization;

namespace WebPerformanceCalculator.Models
{
    public class ProbabilityGraphModel
    {
        [JsonPropertyName("x")]
        public double Time { get; set; }

        [JsonPropertyName("y")]
        public double Probability { get; set; }
    }
}
