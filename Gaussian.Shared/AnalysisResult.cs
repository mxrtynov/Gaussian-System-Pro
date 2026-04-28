using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussian.Shared
{
    public class AnalysisResult
    {
        public int Size { get; set; }
        public double SeqTime { get; set; }
        public double ParTime { get; set; }
        public double Acceleration { get; set; }
        public double Efficiency { get; set; }
        public double TheoreticalMax { get; set; }
    }
}
