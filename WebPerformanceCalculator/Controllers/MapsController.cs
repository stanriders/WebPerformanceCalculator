using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebPerformanceCalculator.Models;
using WebPerformanceCalculator.Parsers;
using WebPerformanceCalculator.Services;

namespace WebPerformanceCalculator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapsController : ControllerBase
    {
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

            if (!uint.TryParse(model.Map, out var mapId))
            {
                var parsedLink = BeatmapLinkParser.Parse(model.Map);
                if (parsedLink == null)
                    return StatusCode(400, new { err = "Incorrect beatmap link!" });

                if (parsedLink.IsBeatmapset)
                    return StatusCode(400, new { err = "Beatmap set links aren't supported" });

                mapId = parsedLink.Id;
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
    }
}
