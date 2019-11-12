
using Newtonsoft.Json;

namespace WebPerformanceCalculator.Models
{
    public class ProbabilityGraphModel
    {
        [JsonProperty("x")]
        public double Time;

        [JsonProperty("y")]
        public double Probability;
    }
}
