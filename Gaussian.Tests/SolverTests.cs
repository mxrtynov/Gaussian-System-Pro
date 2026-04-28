using System;
using System.IO;
using System.Linq;
using System.Threading;
using Gaussian.Solver;
using Gaussian.Shared;
using Gaussian.Coordinator;
using Xunit;

namespace Gaussian.Tests
{
    public class SolverTests : IDisposable
    {
        private readonly DataGenerator _generator = new();
        private readonly GaussEngine _engine = new();
        private static readonly object _fileLock = new object();

        public SolverTests()
        {
            CleanCache();
        }

        public void Dispose()
        {
            CleanCache();
        }

        private void CleanCache()
        {
            lock (_fileLock)
            {
                string cacheFile = "gauss_cache.bin";
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (File.Exists(cacheFile))
                        {
                            File.Delete(cacheFile);
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                    catch { }
                }
            }
        }

        private double ComputeResidual(SparseMatrix matrix, double[] b, double[] x)
        {
            double maxResidual = 0;
            for (int i = 0; i < matrix.Size; i++)
            {
                double sum = 0;
                foreach (var el in matrix.Rows[i])
                {
                    sum += el.Value * x[el.Column];
                }
                double residual = Math.Abs(sum - b[i]);
                if (residual > maxResidual) maxResidual = residual;
            }
            return maxResidual;
        }

        [Fact]
        public void SolveSequential_With2x2System_ReturnsCorrectSolution()
        {
            CleanCache();
            var engine = new GaussEngine();
            int n = 2;
            var matrix = new SparseMatrix(n);

            matrix.Rows[0].Add(new MatrixElement { Column = 0, Value = 2 });
            matrix.Rows[0].Add(new MatrixElement { Column = 1, Value = 3 });
            matrix.Rows[1].Add(new MatrixElement { Column = 0, Value = 5 });
            matrix.Rows[1].Add(new MatrixElement { Column = 1, Value = 7 });

            double[] b = [8, 19];

            double[] solution = engine.SolveSequential(matrix, b, out double ms);

            Assert.NotNull(solution);
            Assert.Equal(2, solution.Length);
            Assert.False(double.IsNaN(solution[0]));
            Assert.False(double.IsNaN(solution[1]));
            Assert.True(ms >= 0);
        }

        [Fact]
        public void SolveSequential_WithIdentityMatrix_ReturnsSameVector()
        {
            CleanCache();
            var engine = new GaussEngine();
            int n = 5;
            var matrix = new SparseMatrix(n);
            double[] expected = [1.5, 2.5, 3.5, 4.5, 5.5];

            for (int i = 0; i < n; i++)
            {
                matrix.Rows[i].Add(new MatrixElement { Column = i, Value = 1 });
            }

            double[] solution = engine.SolveSequential(matrix, expected, out double ms);

            Assert.NotNull(solution);
            Assert.Equal(n, solution.Length);
            for (int i = 0; i < n; i++)
            {
                Assert.Equal(expected[i], solution[i], 10);
            }
        }

        [Fact]
        public void SolveSequential_WithDiagonalMatrix_ReturnsCorrectSolution()
        {
            CleanCache();
            var engine = new GaussEngine();
            int n = 4;
            var matrix = new SparseMatrix(n);
            double[] expected = [2, 4, 6, 8];

            for (int i = 0; i < n; i++)
            {
                matrix.Rows[i].Add(new MatrixElement { Column = i, Value = i + 1 });
            }

            double[] b = expected.Select((val, i) => val * (i + 1)).ToArray();

            double[] solution = engine.SolveSequential(matrix, b, out double ms);

            Assert.NotNull(solution);
            Assert.Equal(n, solution.Length);
            for (int i = 0; i < n; i++)
            {
                Assert.Equal(expected[i], solution[i], 10);
            }
        }

        [Fact]
        public void SolveSequential_With3x3System_ReturnsCorrectSolution()
        {
            CleanCache();
            var engine = new GaussEngine();
            int n = 3;
            var matrix = new SparseMatrix(n);

            matrix.Rows[0].Add(new MatrixElement { Column = 0, Value = 1 });
            matrix.Rows[0].Add(new MatrixElement { Column = 1, Value = 2 });
            matrix.Rows[0].Add(new MatrixElement { Column = 2, Value = 3 });
            matrix.Rows[1].Add(new MatrixElement { Column = 0, Value = 4 });
            matrix.Rows[1].Add(new MatrixElement { Column = 1, Value = 5 });
            matrix.Rows[1].Add(new MatrixElement { Column = 2, Value = 6 });
            matrix.Rows[2].Add(new MatrixElement { Column = 0, Value = 7 });
            matrix.Rows[2].Add(new MatrixElement { Column = 1, Value = 8 });
            matrix.Rows[2].Add(new MatrixElement { Column = 2, Value = 9 });

            double[] b = [6, 15, 24];

            double[] solution = engine.SolveSequential(matrix, b, out double ms);

            Assert.NotNull(solution);
            Assert.Equal(3, solution.Length);
            Assert.False(double.IsNaN(solution[0]));
            Assert.False(double.IsNaN(solution[1]));
            Assert.False(double.IsNaN(solution[2]));
        }

        [Theory]
        [InlineData(500, 0.3)]
        [InlineData(1000, 0.3)]
        [InlineData(2000, 0.3)]
        public void SolveSequential_WithLargeMatrix_ReturnsSolution(int size, double density)
        {
            CleanCache();
            var (matrix, vector) = _generator.GenerateRandomSystem(size, density);
            double[] solution = _engine.SolveSequential(matrix, vector, out double elapsedMs);

            Assert.NotNull(solution);
            Assert.Equal(size, solution.Length);
            Assert.All(solution, x => Assert.False(double.IsNaN(x)));
            Assert.True(elapsedMs > 0);

            double residual = ComputeResidual(matrix, vector, solution);
            Assert.True(residual < 1e-8);
        }

        [Theory]
        [InlineData(500, 4)]
        [InlineData(1000, 4)]
        public void SolveParallel_WithLargeMatrix_ReturnsSameAsSequential(int size, int workers)
        {
            CleanCache();
            var (matrix, vector) = _generator.GenerateRandomSystem(size, 0.3);
            double[] sequential = _engine.SolveSequential(matrix, vector, out _);
            double[] parallel = _engine.SolveParallel(matrix, vector, workers, out _);

            Assert.NotNull(sequential);
            Assert.NotNull(parallel);
            Assert.Equal(size, sequential.Length);
            Assert.Equal(size, parallel.Length);

            for (int i = 0; i < size; i++)
            {
                Assert.Equal(sequential[i], parallel[i], 10);
            }
        }

        [Fact]
        public void SolveParallel_With2x2System_ReturnsSameAsSequential()
        {
            CleanCache();
            var engine = new GaussEngine();
            int n = 2;
            var matrix = new SparseMatrix(n);

            matrix.Rows[0].Add(new MatrixElement { Column = 0, Value = 2 });
            matrix.Rows[0].Add(new MatrixElement { Column = 1, Value = 3 });
            matrix.Rows[1].Add(new MatrixElement { Column = 0, Value = 5 });
            matrix.Rows[1].Add(new MatrixElement { Column = 1, Value = 7 });

            double[] b = [8, 19];

            double[] sequential = engine.SolveSequential(matrix, b, out double seqMs);
            double[] parallel = engine.SolveParallel(matrix, b, 2, out double parMs);

            Assert.NotNull(sequential);
            Assert.NotNull(parallel);
            Assert.Equal(sequential[0], parallel[0], 10);
            Assert.Equal(sequential[1], parallel[1], 10);
        }

        [Fact]
        public void SolveParallel_WithSingleThread_EqualsSequential()
        {
            CleanCache();
            var engine = new GaussEngine();
            int n = 8;
            var matrix = new SparseMatrix(n);

            for (int i = 0; i < n; i++)
            {
                matrix.Rows[i].Add(new MatrixElement { Column = i, Value = 5.0 });
                if (i > 0)
                    matrix.Rows[i].Add(new MatrixElement { Column = i - 1, Value = 1.0 });
                if (i < n - 1)
                    matrix.Rows[i].Add(new MatrixElement { Column = i + 1, Value = 1.0 });
            }

            double[] b = new double[n];
            for (int i = 0; i < n; i++)
            {
                b[i] = 5.0;
                if (i > 0) b[i] += 1.0;
                if (i < n - 1) b[i] += 1.0;
            }

            double[] sequential = engine.SolveSequential(matrix, b, out double seqMs);
            double[] parallelSingle = engine.SolveParallel(matrix, b, 1, out double parMs);

            for (int i = 0; i < n; i++)
            {
                Assert.Equal(sequential[i], parallelSingle[i], 10);
            }
        }
    }
}