﻿
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WebPerformanceCalculator.Services
{
    public class CalculationService
    {
        protected readonly string workingDirectory;
        protected readonly string calculatorPath;

        public CalculationService(IConfiguration _configuration)
        {
            workingDirectory = _configuration["CalculatorPath"];
            if (string.IsNullOrEmpty(workingDirectory))
                workingDirectory = new FileInfo(typeof(Program).Assembly.Location).DirectoryName ?? ".";

            calculatorPath = Path.Combine(workingDirectory, "PerformanceCalculator.dll");
        }
    }
}
