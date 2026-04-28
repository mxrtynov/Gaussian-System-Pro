using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussian.Shared
{
    public class SolveResponse
    {
        public double[]? Solution { get; set; }
        public int Size { get; set; }
        public double ExecutionTimeMs { get; set; }
        public double SequentialTimeMs { get; set; }
        public double ParallelTimeMs { get; set; }
        public double Acceleration { get; set; }
        public double Efficiency { get; set; }
        public double TheoreticalMax { get; set; }
        public int Cores { get; set; }
        public double MemoryUsageMb { get; set; }
        public double Residual { get; set; }
        public string Log { get; set; } = "";
    }
}
