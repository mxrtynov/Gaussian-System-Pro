using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussian.Shared
{
    public class SessionStats
    {
        public int TaskSize { get; set; }
        public double SequentialTimeMs { get; set; }
        public double ParallelTimeMs { get; set; }
        public double Acceleration { get; set; }
        public double Efficiency { get; set; }
        public DateTime Timestamp { get; set; }
        public double ArrivalRate { get; set; }
        public double ServiceRate { get; set; }
        public double ServiceTimeMs { get; set; }
        public double NodeLoadPercent { get; set; }
        public int WorkersCount { get; set; }
        public double Residual { get; set; }
    }
}
