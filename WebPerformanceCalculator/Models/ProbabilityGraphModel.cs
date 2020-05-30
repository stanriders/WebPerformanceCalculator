
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

    public class IndexGraphModel
    {
        [JsonPropertyName("x")]
        public double Time { get; set; }

        [JsonPropertyName("y")]
        public double IPCorrected { get; set; }
    }

    public class FingerGraphModel
    {
        [JsonPropertyName("x")]
        public double Time { get; set; }

        [JsonPropertyName("y")]
        public double Difficulty { get; set; }
    }

    public class TapGraphModel
    {
        [JsonPropertyName("x")]
        public double Time { get; set; }

        [JsonPropertyName("y")]
        public double Difficulty { get; set; }
    }
}
