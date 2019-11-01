using System;
using System.Collections.Generic;
using System.Text;

namespace WebPerformanceCalculator.Worker
{
    public class WorkerDataModel
    {
        public bool NeedsCalcUpdate { get; set; }
        public string Data { get; set; }
    }
}
