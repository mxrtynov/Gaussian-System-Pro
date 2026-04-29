using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Gaussian.Shared;

namespace Gaussian.Solver
{
    public class GaussEngine
    {
        private double[,] ConvertToDense(SparseMatrix matrix)
        {
            int n = matrix.Size;
            double[,] dense = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dense[i, j] = 0;
                }
            }

            for (int i = 0; i < n; i++)
            {
                foreach (var element in matrix.Rows[i])
                {
                    dense[i, element.Column] = element.Value;
                }
            }

            return dense;
        }

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
            double[,] dense = ConvertToDense(matrix);
            return SolveSequential(dense, b, out ms);
        }

        public double[] SolveParallel(SparseMatrix matrix, double[] b, int threads, out double ms)
        {
            int n = matrix.Size;
            double[,] a = ConvertToDense(matrix);
            double[] bCopy = (double[])b.Clone();

            Stopwatch sw = Stopwatch.StartNew();


            for (int i = 0; i < n; i++)
            {

                Parallel.For(i + 1, n, new ParallelOptions { MaxDegreeOfParallelism = threads }, j =>
                {
                    double factor = a[j, i] / a[i, i];
                    for (int k = i; k < n; k++)
                    {
                        a[j, k] -= factor * a[i, k];
                    }
                    bCopy[j] -= factor * bCopy[i];
                });
            }


            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++)
                    sum += a[i, j] * x[j];
                x[i] = (bCopy[i] - sum) / a[i, i];
            }

            sw.Stop();
            ms = sw.Elapsed.TotalMilliseconds;
            return x;
        }
    }
}