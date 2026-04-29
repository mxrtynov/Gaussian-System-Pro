using Gaussian.Coordinator;
using Gaussian.Shared;
using Xunit;
using System.Linq;

namespace Gaussian.Tests;

public class DataGeneratorTests
{
    private readonly DataGenerator _generator = new();

    [Fact]
    public void GenerateRandomSystem_WithValidParameters_ReturnsMatrixAndVector()
    {
        int size = 10;
        double density = 0.3;

        var (matrix, vector) = _generator.GenerateRandomSystem(size, density);

        Assert.Equal(size, matrix.Size);
        Assert.Equal(size, vector.Length);
        Assert.NotNull(matrix.Rows);
    }

    [Fact]
    public void GenerateRandomSystem_WithFullDensity_ReturnsFullMatrix()
    {
        int size = 5;
        double density = 1.0;

        var (matrix, vector) = _generator.GenerateRandomSystem(size, density);

        for (int i = 0; i < size; i++)
        {
            Assert.Equal(size, matrix.Rows[i].Count);
        }
    }

    [Fact]
    public void GenerateRandomSystem_WithLowDensity_ReturnsSparseMatrix()
    {
        int size = 20;
        double density = 0.1;

        var (matrix, vector) = _generator.GenerateRandomSystem(size, density);

        int totalNonZero = 0;
        for (int i = 0; i < size; i++)
        {
            totalNonZero += matrix.Rows[i].Count;
        }

        double actualDensity = (double)totalNonZero / (size * size);
        Assert.True(actualDensity <= density + 0.15);
    }

    [Fact]
    public void GenerateRandomSystem_WithZeroDensity_ReturnsOnlyDiagonal()
    {
        int size = 10;
        double density = 0.0;

        var (matrix, vector) = _generator.GenerateRandomSystem(size, density);

        for (int i = 0; i < size; i++)
        {
            Assert.Contains(matrix.Rows[i], e => e.Column == i);
        }
    }

    [Fact]
    public void GenerateRandomSystem_WithDifferentDensities_ReturnsDifferentSparsity()
    {
        int size = 20;

        var (matrixLow, _) = _generator.GenerateRandomSystem(size, 0.1);
        var (matrixHigh, _) = _generator.GenerateRandomSystem(size, 0.5);

        int lowNonZero = 0;
        int highNonZero = 0;

        for (int i = 0; i < size; i++)
        {
            lowNonZero += matrixLow.Rows[i].Count;
            highNonZero += matrixHigh.Rows[i].Count;
        }

        Assert.True(lowNonZero < highNonZero);
    }

    [Fact]
    public void GenerateRandomSystem_WithSize100_WorksWithoutError()
    {
        var (matrix, vector) = _generator.GenerateRandomSystem(100, 0.2);

        Assert.Equal(100, matrix.Size);
        Assert.Equal(100, vector.Length);
    }

    [Fact]
    public void GenerateRandomSystem_WithDensityZeroPointFive_ReturnsReasonableSparsity()
    {
        int size = 50;
        double density = 0.5;

        var (matrix, vector) = _generator.GenerateRandomSystem(size, density);

        int totalElements = size * size;
        int nonZeroElements = 0;
        for (int i = 0; i < size; i++)
        {
            nonZeroElements += matrix.Rows[i].Count;
        }

        double actualDensity = (double)nonZeroElements / totalElements;
        Assert.InRange(actualDensity, 0.4, 0.6);
    }

    [Fact]
    public void GenerateRandomSystem_ReturnsVectorOfCorrectSize()
    {
        int size = 15;
        var (_, vector) = _generator.GenerateRandomSystem(size, 0.3);

        Assert.Equal(size, vector.Length);
    }

    [Fact]
    public void GenerateRandomSystem_ReturnsNonEmptyRows()
    {
        int size = 10;
        var (matrix, _) = _generator.GenerateRandomSystem(size, 0.2);

        for (int i = 0; i < size; i++)
        {
            Assert.NotEmpty(matrix.Rows[i]);
        }
    }

    [Fact]
    public void GenerateRandomSystem_WithSize1_ReturnsSingleElement()
    {
        var (matrix, vector) = _generator.GenerateRandomSystem(1, 0.5);

        Assert.Single(matrix.Rows);
        Assert.Single(vector);
        Assert.Single(matrix.Rows[0]);
    }

    [Fact]
    public void GenerateRandomSystem_ValuesAreWithinExpectedRange()
    {
        int size = 100;
        var (matrix, vector) = _generator.GenerateRandomSystem(size, 0.3);

        for (int i = 0; i < size; i++)
        {
            foreach (var el in matrix.Rows[i])
            {
                Assert.True(Math.Abs(el.Value) < 1000, $"Value {el.Value} out of range");
            }
            Assert.True(Math.Abs(vector[i]) < 1000, $"Vector {vector[i]} out of range");
        }
    }

    [Fact]
    public void GenerateRandomSystem_WithSameParameters_ReturnsDifferentSystems()
    {
        int size = 10;
        double density = 0.3;

        var (matrix1, vector1) = _generator.GenerateRandomSystem(size, density);
        var (matrix2, vector2) = _generator.GenerateRandomSystem(size, density);

        bool hasDifference = false;
        for (int i = 0; i < size && !hasDifference; i++)
        {
            if (matrix1.Rows[i].Count != matrix2.Rows[i].Count)
                hasDifference = true;
            else if (vector1[i] != vector2[i])
                hasDifference = true;
        }
        Assert.True(hasDifference);
    }

    [Fact]
    public void GenerateRandomSystem_HandlesLargeSize()
    {
        var (matrix, vector) = _generator.GenerateRandomSystem(200, 0.1);

        Assert.Equal(200, matrix.Size);
        Assert.Equal(200, vector.Length);
    }

    [Fact]
    public void GenerateRandomSystem_DiagonalElementAlwaysPresent()
    {
        int size = 20;
        var (matrix, _) = _generator.GenerateRandomSystem(size, 0.05);

        for (int i = 0; i < size; i++)
        {
            Assert.Contains(matrix.Rows[i], e => e.Column == i);
        }
    }

    [Fact]
    public void GenerateRandomSystem_ColumnIndicesAreValid()
    {
        int size = 15;
        var (matrix, _) = _generator.GenerateRandomSystem(size, 0.3);

        for (int i = 0; i < size; i++)
        {
            foreach (var el in matrix.Rows[i])
            {
                Assert.InRange(el.Column, 0, size - 1);
            }
        }
    }
}