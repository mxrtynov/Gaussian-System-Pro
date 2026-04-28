using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussian.Shared
{
    public class SolveTask
    {
        public string MatrixPath { get; set; } = "";
        public string VectorPath { get; set; } = "";
        public int Size { get; set; }
        public string Name { get; set; } = "";
    }
}
