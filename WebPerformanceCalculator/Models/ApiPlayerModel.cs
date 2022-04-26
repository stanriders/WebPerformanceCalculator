
using Newtonsoft.Json;

namespace WebPerformanceCalculator.Models;

public class ApiPlayerModel
{
    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("user_id")]
    public int Id { get; set; }
    
    [JsonProperty("username")]
    public string? Username { get; set; }
    
    [JsonProperty("pp_raw")]
    public double Pp { get; set; }
}