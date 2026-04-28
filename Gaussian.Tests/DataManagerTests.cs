using Gaussian.Coordinator;
using Gaussian.Shared;
using Xunit;
using System.IO;
using System.Linq;

namespace Gaussian.Tests;

public class DataManagerTests
{
    private readonly string _testDir;

    public DataManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DataManagerTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    private string CreateMatrixFile(double[][] data)
    {
        string path = Path.Combine(_testDir, "matrix.txt");
        var lines = data.Select(row => string.Join(" ", row));
        File.WriteAllLines(path, lines);
        return path;
    }

    private string CreateVectorFile(double[] data)
    {
        string path = Path.Combine(_testDir, "vector.txt");
        File.WriteAllLines(path, data.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void LoadFiles_WithValidFiles_ReturnsMatrixAndVector()
    {
        double[][] matrixData = [[1, 2, 3], [4, 5, 6], [7, 8, 9]];
        double[] vectorData = [10, 11, 12];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(3, matrix.Size);
        Assert.Equal(3, vector.Length);
        Assert.Equal(10, vector[0]);
        Assert.Equal(12, vector[2]);
    }

    [Fact]
    public void LoadFiles_WithSparseMatrix_HandlesZeros()
    {
        double[][] matrixData = [[1, 0, 3], [0, 5, 0], [7, 0, 9]];
        double[] vectorData = [10, 11, 12];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(2, matrix.Rows[0].Count);
        Assert.Equal(1, matrix.Rows[1].Count);
        Assert.Equal(2, matrix.Rows[2].Count);
        Assert.Equal(5, matrix.Rows[1][0].Value);
    }

    [Fact]
    public void LoadFiles_WithSpacesAndTabs_HandlesCorrectly()
    {
        string matrixPath = Path.Combine(_testDir, "matrix.txt");
        string vectorPath = Path.Combine(_testDir, "vector.txt");

        File.WriteAllText(matrixPath, "1\t2 3\n4 5\t6\n7\t8\t9");
        File.WriteAllText(vectorPath, "10\n11\n12");

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(3, matrix.Size);
        Assert.Equal(3, matrix.Rows[0].Count);
        Assert.Equal(3, matrix.Rows[1].Count);
        Assert.Equal(3, matrix.Rows[2].Count);
    }

    [Fact]
    public void LoadFiles_WithDecimalNumbers_HandlesCorrectly()
    {
        double[][] matrixData = [[1.5, 2.7, 3.9], [4.2, 5.3, 6.4]];
        double[] vectorData = [10.1, 11.2];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(2, matrix.Size);
        Assert.Equal(10.1, vector[0], 5);
        Assert.Equal(11.2, vector[1], 5);
    }

    [Fact]
    public void LoadFiles_WithNegativeNumbers_HandlesCorrectly()
    {
        double[][] matrixData = [[-1, -2, -3], [-4, -5, -6]];
        double[] vectorData = [-10, -11];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(-10, vector[0]);
        Assert.Equal(-11, vector[1]);
        Assert.Equal(-1, matrix.Rows[0][0].Value);
        Assert.Equal(-2, matrix.Rows[0][1].Value);
    }

    [Fact]
    public void LoadFiles_WithEmptyLines_SkipsThem()
    {
        string matrixPath = Path.Combine(_testDir, "matrix.txt");
        string vectorPath = Path.Combine(_testDir, "vector.txt");

        File.WriteAllText(matrixPath, "\n1 2\n\n3 4\n");
        File.WriteAllText(vectorPath, "5\n6");

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(2, matrix.Size);
        Assert.Equal(2, vector.Length);
    }

    [Fact]
    public void LoadFiles_WithLargeMatrix_HandlesCorrectly()
    {
        int size = 50;
        double[][] matrixData = new double[size][];
        for (int i = 0; i < size; i++)
        {
            matrixData[i] = Enumerable.Range(0, size).Select(j => (double)(i + j)).ToArray();
        }
        double[] vectorData = Enumerable.Range(0, size).Select(i => (double)i).ToArray();

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(size, matrix.Size);
        Assert.Equal(size, vector.Length);
        Assert.Equal(0, vector[0]);
        Assert.Equal(size - 1, vector[size - 1]);
    }

    [Fact]
    public void LoadFiles_WithMissingMatrixFile_ThrowsException()
    {
        string vectorPath = CreateVectorFile([1, 2, 3]);

        Assert.Throws<FileNotFoundException>(() =>
            DataManager.LoadFiles(Path.Combine(_testDir, "missing.txt"), vectorPath));
    }

    [Fact]
    public void LoadFiles_WithMissingVectorFile_ThrowsException()
    {
        double[][] matrixData = [[1, 2], [3, 4]];
        string matrixPath = CreateMatrixFile(matrixData);

        Assert.Throws<FileNotFoundException>(() =>
            DataManager.LoadFiles(matrixPath, Path.Combine(_testDir, "missing.txt")));
    }

    [Fact]
    public void LoadFiles_WithInvalidNumberFormat_ThrowsException()
    {
        string matrixPath = Path.Combine(_testDir, "matrix.txt");
        string vectorPath = Path.Combine(_testDir, "vector.txt");

        File.WriteAllText(matrixPath, "a b c\n1 2 3");
        File.WriteAllText(vectorPath, "1\n2\n3");

        Assert.Throws<System.FormatException>(() =>
            DataManager.LoadFiles(matrixPath, vectorPath));
    }

    [Fact]
    public void LoadFiles_WithEmptyMatrix_ReturnsEmptyRows()
    {
        string matrixPath = Path.Combine(_testDir, "matrix.txt");
        string vectorPath = CreateVectorFile([1, 2, 3]);

        File.WriteAllText(matrixPath, "");

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(3, matrix.Size);
        for (int i = 0; i < 3; i++)
        {
            Assert.Empty(matrix.Rows[i]);
        }
    }

    [Fact]
    public void LoadFiles_WithSingleElementMatrix_ReturnsSize1()
    {
        double[][] matrixData = [[42]];
        double[] vectorData = [84];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(1, matrix.Size);
        Assert.Single(vector);
        Assert.Equal(42, matrix.Rows[0][0].Value);
        Assert.Equal(84, vector[0]);
    }

    [Fact]
    public void LoadFiles_WithVerySmallValues_PreservesPrecision()
    {
        double[][] matrixData = [[1e-10, 2e-10], [3e-10, 4e-10]];
        double[] vectorData = [5e-10, 6e-10];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(1e-10, matrix.Rows[0][0].Value, 15);
        Assert.Equal(5e-10, vector[0], 15);
    }

    [Fact]
    public void LoadFiles_WithVeryLargeValues_HandlesCorrectly()
    {
        double[][] matrixData = [[1e10, 2e10], [3e10, 4e10]];
        double[] vectorData = [5e10, 6e10];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);

        Assert.Equal(1e10, matrix.Rows[0][0].Value, 5);
        Assert.Equal(5e10, vector[0], 5);
    }

    [Fact]
    public void LoadFiles_FiltersOutNearZeroValues()
    {
        double[][] matrixData = [[1e-14, 5, 1e-13], [0, 0, 0]];
        double[] vectorData = [1, 2];

        string matrixPath = CreateMatrixFile(matrixData);
        string vectorPath = CreateVectorFile(vectorData);

        var (matrix, vector) = DataManager.LoadFiles(matrixPath, vectorPath);


        Assert.DoesNotContain(matrix.Rows[0], e => Math.Abs(e.Value) < 1e-14);
    }
}