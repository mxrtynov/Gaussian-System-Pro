using System;
using System.IO;
using Gaussian.Coordinator;
using Gaussian.Shared;
using Xunit;

namespace Gaussian.Coordinator.Tests
{
    public class DataManagerTests
    {
        [Fact]
        public void LoadFiles_ValidMatrixAndVector_ReturnsCorrectDimensions()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 2 3", "4 5 6", "7 8 9" });
            File.WriteAllLines(vectorPath, new[] { "1.0", "2.0", "3.0" });
            var manager = new DataManager();

            var (matrix, b) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(3, matrix.Rows.Length);
            Assert.Equal(3, b.Length);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_SparseMatrixWithZeros_OnlyNonZeroElementsStored()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 0 0", "0 5 0", "0 0 9" });
            File.WriteAllLines(vectorPath, new[] { "1", "2", "3" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Single(matrix.Rows[0]);
            Assert.Single(matrix.Rows[1]);
            Assert.Single(matrix.Rows[2]);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_VectorWithFloatingPoint_ParsesCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 0" });
            File.WriteAllLines(vectorPath, new[] { "1.5", "2.7" });
            var manager = new DataManager();

            var (_, b) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(1.5, b[0]);
            Assert.Equal(2.7, b[1]);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_MatrixWithNegativeValues_StoresCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "-1 2 -3", "4 -5 6" });
            File.WriteAllLines(vectorPath, new[] { "0", "0" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(-1, matrix.Rows[0][0].Value);
            Assert.Equal(2, matrix.Rows[0][1].Value);
            Assert.Equal(-3, matrix.Rows[0][2].Value);
            Assert.Equal(4, matrix.Rows[1][0].Value);
            Assert.Equal(-5, matrix.Rows[1][1].Value);
            Assert.Equal(6, matrix.Rows[1][2].Value);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_MatrixWithTabSeparators_HandlesCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1\t2\t3", "4\t5\t6" });
            File.WriteAllLines(vectorPath, new[] { "1", "2" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(3, matrix.Rows[0].Count);
            Assert.Equal(3, matrix.Rows[1].Count);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_MatrixWithEmptyLines_SkipsThem()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 2", "", "3 4", "   ", "5 6" });
            File.WriteAllLines(vectorPath, new[] { "1", "2", "3" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(3, matrix.Rows.Length);
            Assert.Equal(2, matrix.Rows[0].Count);
            Assert.Equal(2, matrix.Rows[1].Count);
            Assert.Equal(2, matrix.Rows[2].Count);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_MatrixWithScientificNotation_ParsesCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1e-3 2.5E+2" });
            File.WriteAllLines(vectorPath, new[] { "0", "0" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(0.001, matrix.Rows[0][0].Value);
            Assert.Equal(250, matrix.Rows[0][1].Value);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_RowCountMatchesVectorLength_ReturnsConsistentData()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 2", "3 4", "5 6", "7 8" });
            File.WriteAllLines(vectorPath, new[] { "1", "2", "3", "4" });
            var manager = new DataManager();

            var (matrix, b) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(4, matrix.Rows.Length);
            Assert.Equal(4, b.Length);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_VectorWithDifferentLength_ThrowsIndexOutOfRange()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 2", "3 4" });
            File.WriteAllLines(vectorPath, new[] { "1" });
            var manager = new DataManager();

            Assert.Throws<IndexOutOfRangeException>(() => manager.LoadFiles(matrixPath, vectorPath));
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_ValuesBelowTolerance_FilteredOut()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 1e-16 2" });
            File.WriteAllLines(vectorPath, new[] { "0" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(2, matrix.Rows[0].Count);
            Assert.Equal(1, matrix.Rows[0][0].Value);
            Assert.Equal(2, matrix.Rows[0][1].Value);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_MatrixWithMixedSpacingAndTabs_HandlesCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1   2\t3", "4\t5   6" });
            File.WriteAllLines(vectorPath, new[] { "1", "2" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(3, matrix.Rows[0].Count);
            Assert.Equal(3, matrix.Rows[1].Count);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_VectorWithInvariantCulture_ParsesDecimalPointCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "1 1" });
            File.WriteAllLines(vectorPath, new[] { "123.456", "789.012" });
            var manager = new DataManager();

            var (_, b) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(123.456, b[0]);
            Assert.Equal(789.012, b[1]);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_ColumnIndexesPreserved_OrderCorrect()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "5 0 3 0 1" });
            File.WriteAllLines(vectorPath, new[] { "0" });
            var manager = new DataManager();

            var (matrix, _) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Equal(0, matrix.Rows[0][0].Column);
            Assert.Equal(2, matrix.Rows[0][1].Column);
            Assert.Equal(4, matrix.Rows[0][2].Column);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_LargeMatrix_HandlesWithoutErrors()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            var matrixLines = new string[100];
            var vectorLines = new string[100];
            for (int i = 0; i < 100; i++)
            {
                matrixLines[i] = string.Join(" ", Enumerable.Range(0, 100).Select(j => (i * j) % 10));
                vectorLines[i] = "1";
            }
            File.WriteAllLines(matrixPath, matrixLines);
            File.WriteAllLines(vectorPath, vectorLines);
            var manager = new DataManager();

            var exception = Record.Exception(() => manager.LoadFiles(matrixPath, vectorPath));

            Assert.Null(exception);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }

        [Fact]
        public void LoadFiles_SingleElementMatrix_WorksCorrectly()
        {
            var matrixPath = Path.GetTempFileName();
            var vectorPath = Path.GetTempFileName();
            File.WriteAllLines(matrixPath, new[] { "42" });
            File.WriteAllLines(vectorPath, new[] { "7" });
            var manager = new DataManager();

            var (matrix, b) = manager.LoadFiles(matrixPath, vectorPath);

            Assert.Single(matrix.Rows);
            Assert.Single(b);
            Assert.Equal(42, matrix.Rows[0][0].Value);
            Assert.Equal(7, b[0]);
            File.Delete(matrixPath);
            File.Delete(vectorPath);
        }
    }
}