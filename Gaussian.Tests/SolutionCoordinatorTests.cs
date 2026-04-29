using System;
using System.IO;
using System.Linq;
using Gaussian.Shared;
using Gaussian.Coordinator;
using Xunit;

namespace Gaussian.Tests
{
    public class SolutionCoordinatorTests : IDisposable
    {
        private readonly string _testDir;
        private readonly SolutionCoordinator _coordinator;

        public SolutionCoordinatorTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "GaussianTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _coordinator = new SolutionCoordinator();
        }

        public void Dispose()
        {
            _coordinator?.Shutdown();
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); }
                catch { }
            }
        }

        private (string matrixPath, string vectorPath) CreateTestFiles(int size, double diagValue = 5.0)
        {
            string matrixPath = Path.Combine(_testDir, $"matrix_{size}.txt");
            string vectorPath = Path.Combine(_testDir, $"vector_{size}.txt");

            var matrixLines = new string[size];
            for (int i = 0; i < size; i++)
            {
                var row = new double[size];
                row[i] = diagValue;
                matrixLines[i] = string.Join(" ", row.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            File.WriteAllLines(matrixPath, matrixLines);

            var vectorLines = Enumerable.Repeat(diagValue.ToString(System.Globalization.CultureInfo.InvariantCulture), size).ToArray();
            File.WriteAllLines(vectorPath, vectorLines);

            return (matrixPath, vectorPath);
        }

        [Fact]
        public void Constructor_DoesNotThrow()
        {
            var coordinator = new SolutionCoordinator();
            Assert.NotNull(coordinator);
        }

        [Fact]
        public void Shutdown_WithNoWorkers_DoesNotThrow()
        {
            var exception = Record.Exception(() => _coordinator.Shutdown());
            Assert.Null(exception);
        }

        [Fact]
        public void Shutdown_MultipleCalls_DoesNotThrow()
        {
            _coordinator.Shutdown();
            _coordinator.Shutdown();
            _coordinator.Shutdown();
            Assert.True(true);
        }

        [Fact]
        public void HandleRequest_WithZeroWorkers_UsesAtLeastOne()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 0);

            Assert.NotNull(response);
        }

        [Fact]
        public void HandleRequest_WithNegativeWorkers_UsesOne()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, -5);

            Assert.NotNull(response);
        }

        [Fact]
        public void HandleRequest_ReturnsResponseWithSize()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(3);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 1);

            Assert.Equal(3, response.Size);
        }

        [Fact]
        public void HandleRequest_ReturnsSequentialTime()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 1);

            Assert.True(response.SequentialTimeMs >= 0);
        }

        [Fact]
        public void HandleRequest_ReturnsParallelTime()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 2);

            Assert.True(response.ParallelTimeMs >= 0);
        }

        [Fact]
        public void HandleRequest_ReturnsCoresCount()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 1);

            Assert.NotNull(response);
            Assert.IsType<int>(response.Cores);
        }

        [Fact]
        public void HandleRequest_ReturnsLogOkOnSuccess()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 1);

            Assert.Equal("OK", response.Log);
        }

        [Fact]
        public void HandleRequest_WithMissingMatrix_ReturnsErrorLog()
        {
            var request = new SolveRequest
            {
                MatrixPath = Path.Combine(_testDir, "missing.txt"),
                FreeTermsPath = Path.Combine(_testDir, "vector.txt")
            };
            File.WriteAllText(request.FreeTermsPath, "1.0\n2.0");

            var response = _coordinator.HandleRequest(request, 1);

            Assert.Contains("ERROR", response.Log);
        }

        [Fact]
        public void HandleRequest_WithMissingVector_ReturnsErrorLog()
        {
            var request = new SolveRequest
            {
                MatrixPath = Path.Combine(_testDir, "matrix.txt"),
                FreeTermsPath = Path.Combine(_testDir, "missing.txt")
            };
            File.WriteAllText(request.MatrixPath, "1 2\n3 4");

            var response = _coordinator.HandleRequest(request, 1);

            Assert.Contains("ERROR", response.Log);
        }

        [Fact]
        public void HandleRequest_WithInvalidMatrix_ReturnsErrorLog()
        {
            var (_, vectorPath) = CreateTestFiles(2);
            string matrixPath = Path.Combine(_testDir, "invalid.txt");
            File.WriteAllText(matrixPath, "a b c\nd e f");

            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 1);

            Assert.Contains("ERROR", response.Log);
        }

        [Fact]
        public void HandleRequest_WithInvalidVector_ReturnsErrorLog()
        {
            var (matrixPath, _) = CreateTestFiles(2);
            string vectorPath = Path.Combine(_testDir, "invalid.txt");
            File.WriteAllText(vectorPath, "a\nb");

            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };

            var response = _coordinator.HandleRequest(request, 1);

            Assert.Contains("ERROR", response.Log);
        }

        [Fact]
        public void HandleRequest_WithSize1Matrix_ReturnsSolution()
        {
            string matrixPath = Path.Combine(_testDir, "size1.txt");
            string vectorPath = Path.Combine(_testDir, "vec1.txt");
            File.WriteAllText(matrixPath, "3.0");
            File.WriteAllText(vectorPath, "6.0");

            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };
            var response = _coordinator.HandleRequest(request, 1);

            Assert.NotNull(response.Solution);
            Assert.Single(response.Solution);
            Assert.Equal(2.0, response.Solution[0], 5);
        }

        [Fact]
        public void HandleRequest_WithSize2Diagonal_ReturnsCorrectSolution()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(2, 4.0);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };
            var response = _coordinator.HandleRequest(request, 1);

            Assert.NotNull(response.Solution);
            Assert.Equal(2, response.Solution.Length);
            Assert.Equal(1.0, response.Solution[0], 5);
            Assert.Equal(1.0, response.Solution[1], 5);
        }

        [Fact]
        public void HandleRequest_ResidualIsSmall()
        {
            var (matrixPath, vectorPath) = CreateTestFiles(3, 5.0);
            var request = new SolveRequest { MatrixPath = matrixPath, FreeTermsPath = vectorPath };
            var response = _coordinator.HandleRequest(request, 1);

            Assert.True(response.Residual < 1e-10);
        }


    }
}