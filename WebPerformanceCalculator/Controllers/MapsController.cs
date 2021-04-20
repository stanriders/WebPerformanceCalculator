using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebPerformanceCalculator.Models;
using WebPerformanceCalculator.Services;

namespace WebPerformanceCalculator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapsController : ControllerBase
    {
        private readonly Regex mapLinkRegex =
            new(@"(?>https?:\/\/)?(?>osu|old)\.ppy\.sh\/([b,s]|(?>beatmaps)|(?>beatmapsets))\/(\d+\/?\#osu\/)?(\d+)?\/?(?>[&,?].=\d)?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        private readonly MapCalculationService calculationService;

        public MapsController(MapCalculationService _calculationService)
        {
            calculationService = _calculationService;
        }

        [HttpPost]
        [Route("calculate")]
        public async Task<IActionResult> Calculate(CalculateMapModel model)
        {
            if (string.IsNullOrEmpty(model.Map))
                return StatusCode(400, new { err = "Incorrect beatmap link!" });

            if (!int.TryParse(model.Map, out var mapId))
            {
                mapId = GetMapIdFromLink(model.Map, out var isSet);
                if (mapId == 0)
                    return StatusCode(400, new { err = "Incorrect beatmap link!" });

                if (isSet)
                    return StatusCode(400, new { err = "Beatmap set links aren't supported" });
            }

            var mapInfo = await calculationService.Calculate(mapId, model.Mods);
            if (!string.IsNullOrEmpty(mapInfo))
                return Ok(mapInfo);

            return StatusCode(500, new { err = "Failed to calculate!" });
        }

        [HttpGet("probabilitychart/{mapId}")]
        [Route("probabilitychart")]
        public async Task<IActionResult> GetProbabilityChart(string mapId, string? mods = "")
        {
            var graph = await calculationService.GetProbabilityChart(mapId, mods);
            if (graph?.Length > 0)
            {
                var jsonGraph = new List<ProbabilityGraphModel>(graph.Length);
                foreach (var g in graph)
                {
                    var split = g.Split(' ');
                    jsonGraph.Add(new ProbabilityGraphModel
                    {
                        Time = Convert.ToDouble(split[0]),
                        Probability = Convert.ToDouble(split[3])
                    });
                }

                return Ok(jsonGraph);
            }

            return StatusCode(400);
        }

        /// <summary>
        /// Parse beatmap link
        /// </summary>
        /// <param name="link">Link to parse</param>
        /// <param name="isSet">True if it's a beatmapset link</param>
        /// <returns>Beatmap ID</returns>
        private int GetMapIdFromLink(string link, out bool isSet)
        {
            int beatmapId = 0;
            isSet = false;
            Match regexMatch = mapLinkRegex.Match(link);
            if (regexMatch.Groups.Count > 1)
            {
                List<Group> regexGroups = regexMatch.Groups.Values
                    .Where(x => x.Length > 0)
                    .ToList();

                bool isNew = regexGroups[1].Value == "beatmapsets"; // are we using new website or not
                if (isNew)
                {
                    if (regexGroups[2].Value.Contains("#osu/"))
                        beatmapId = int.Parse(regexGroups[3].Value);
                    else
                        isSet = true;
                }
                else
                {
                    if (regexGroups[1].Value == "s")
                        isSet = true;
                    else
                        beatmapId = int.Parse(regexGroups[2].Value);
                }
            }

            return beatmapId;
        }
    }
}
