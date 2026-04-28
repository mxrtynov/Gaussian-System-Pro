using System;
using System.Diagnostics;
using Gaussian.Shared;

namespace Gaussian.Solver
{
    public class GaussEngine
    {
        public double[] SolveSequential(double[,] matrix, double[] b, out double ms)
        {
            int n = matrix.GetLength(0);
            double[,] a = new double[n, n];
            double[] bCopy = new double[n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    a[i, j] = matrix[i, j];
                }
                bCopy[i] = b[i];
            }

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double factor = a[j, i] / a[i, i];
                    for (int k = i; k < n; k++)
                    {
                        a[j, k] -= factor * a[i, k];
                    }
                    bCopy[j] -= factor * bCopy[i];
                }
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++)
                {
                    sum += a[i, j] * x[j];
                }
                x[i] = (bCopy[i] - sum) / a[i, i];
            }

            sw.Stop();
            ms = sw.Elapsed.TotalMilliseconds;
            return x;
        }

        public double[] SolveSequential(SparseMatrix matrix, double[] b, out double ms)
        {
            int n = matrix.Size;
            double[,] dense = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dense[i, j] = 0;
                }
                foreach (var el in matrix.Rows[i])
                {
                    dense[i, el.Column] = el.Value;
                }
            }

            return SolveSequential(dense, b, out ms);
        }

        public double[] SolveParallel(SparseMatrix matrix, double[] b, int threads, out double ms)
        {
            return SolveSequential(matrix, b, out ms);
        }
    }
}