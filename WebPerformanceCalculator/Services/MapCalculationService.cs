using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RunProcessAsTask;

namespace WebPerformanceCalculator.Services
{
    public class MapCalculationService : CalculationService
    {
        private readonly CalculationUpdatesService updateService;

        public MapCalculationService(CalculationUpdatesService _updateService, IConfiguration _configuration) : base(_configuration)
        {
            updateService = _updateService;
        }

        public async Task<string?> Calculate(uint mapId, string[] mods)
        {
            var modsJoined = string.Join(string.Empty, mods);
            var mapInfoPath = $"{workingDirectory}/mapinfo/{mapId}_{modsJoined}.json";

            if (updateService.IsOutdatedFile(mapInfoPath))
            {
                try
                {
                    var commandMods = string.Empty;
                    if (mods.Length > 0)
                        commandMods = "-m " + string.Join(" -m ", mods);

                    await ProcessEx.RunAsync(new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        WorkingDirectory = workingDirectory,
                        Arguments = $"{calculatorPath} simulate osu cache/{mapId}.osu {commandMods}",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    });

                    if (File.Exists(mapInfoPath))
                        return await File.ReadAllTextAsync(mapInfoPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to calc {mapId}\n {e.Message}");
                }
            }
            else
            {
                return await File.ReadAllTextAsync(mapInfoPath);
            }

            return null;
        }

        public async Task<string[]?> GetProbabilityChart(string mapId, string? mods = "")
        {
            var path = $"{workingDirectory}/cache/graph_{mapId}_{mods}.txt";
            if (File.Exists(path))
                return await File.ReadAllLinesAsync(path);

            return null;
        }
    }
}
