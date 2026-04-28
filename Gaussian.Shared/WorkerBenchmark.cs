using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussian.Shared
{
    public class WorkerBenchmark
    {
        public int WorkerCount { get; set; }
        public double TimeMs { get; set; }
        public int MatrixSize { get; set; }
        public string MatrixName { get; set; } = "";
    }
}
