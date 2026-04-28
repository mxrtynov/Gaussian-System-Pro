using Gaussian.Shared;
using Xunit;
using System;

namespace Gaussian.Tests;

public class ModelsTests
{
    [Fact]
    public void SparseMatrix_Constructor_InitializesCorrectly()
    {
        int size = 5;
        var matrix = new SparseMatrix(size);

        Assert.Equal(size, matrix.Size);
        Assert.NotNull(matrix.Rows);
        Assert.Equal(size, matrix.Rows.Length);

        for (int i = 0; i < size; i++)
        {
            Assert.NotNull(matrix.Rows[i]);
            Assert.Empty(matrix.Rows[i]);
        }
    }

    [Fact]
    public void SparseMatrix_Clone_ReturnsIndependentCopy()
    {
        var original = new SparseMatrix(3);
        original.Rows[0].Add(new MatrixElement { Column = 1, Value = 2.5 });
        original.Rows[0].Add(new MatrixElement { Column = 2, Value = 3.7 });

        var clone = original.Clone();

        Assert.Equal(original.Size, clone.Size);
        Assert.Equal(original.Rows[0].Count, clone.Rows[0].Count);

        clone.Rows[0][0].Value = 999;
        clone.Rows[0].Add(new MatrixElement { Column = 0, Value = 100 });

        Assert.NotEqual(original.Rows[0][0].Value, clone.Rows[0][0].Value);
        Assert.NotEqual(original.Rows[0].Count, clone.Rows[0].Count);
    }

    [Fact]
    public void MatrixElement_Properties_SetAndGet()
    {
        var element = new MatrixElement { Column = 5, Value = 3.14159 };

        Assert.Equal(5, element.Column);
        Assert.Equal(3.14159, element.Value);
    }

    [Fact]
    public void SolveRequest_Properties_SetAndGet()
    {
        var request = new SolveRequest
        {
            MatrixPath = "matrix.txt",
            FreeTermsPath = "vector.txt"
        };

        Assert.Equal("matrix.txt", request.MatrixPath);
        Assert.Equal("vector.txt", request.FreeTermsPath);
    }

    [Fact]
    public void SolveResponse_Properties_SetAndGet()
    {
        var response = new SolveResponse();
        double[] solution = [1.0, 2.0, 3.0];

        response.Solution = solution;
        response.Size = 3;
        response.ParallelTimeMs = 100.5;
        response.SequentialTimeMs = 250.3;
        response.Acceleration = 2.49;
        response.Efficiency = 82.5;
        response.TheoreticalMax = 4.0;
        response.Cores = 8;
        response.MemoryUsageMb = 256.7;
        response.Residual = 1e-12;
        response.Log = "Test log";

        Assert.Equal(solution, response.Solution);
        Assert.Equal(3, response.Size);
        Assert.Equal(100.5, response.ParallelTimeMs);
        Assert.Equal(250.3, response.SequentialTimeMs);
        Assert.Equal(2.49, response.Acceleration);
        Assert.Equal(82.5, response.Efficiency);
        Assert.Equal(4.0, response.TheoreticalMax);
        Assert.Equal(8, response.Cores);
        Assert.Equal(256.7, response.MemoryUsageMb);
        Assert.Equal(1e-12, response.Residual);
        Assert.Equal("Test log", response.Log);
    }

    [Fact]
    public void SolveResponse_DefaultValues_AreDefault()
    {
        var response = new SolveResponse();

        Assert.Null(response.Solution);
        Assert.Equal(0, response.Size);
        Assert.Equal(0, response.ParallelTimeMs);
        Assert.Equal(0, response.SequentialTimeMs);
        Assert.Equal(0, response.Acceleration);
        Assert.Equal(0, response.Efficiency);
        Assert.Equal(0, response.TheoreticalMax);
        Assert.Equal(0, response.Cores);
        Assert.Equal(0, response.MemoryUsageMb);
        Assert.Equal(0, response.Residual);
        Assert.Equal("", response.Log);
    }

    [Fact]
    public void AnalysisResult_Properties_SetAndGet()
    {
        var result = new AnalysisResult
        {
            Size = 1000,
            SeqTime = 2500.5,
            ParTime = 625.1,
            Acceleration = 4.0,
            Efficiency = 80.0,
            TheoreticalMax = 8.0
        };

        Assert.Equal(1000, result.Size);
        Assert.Equal(2500.5, result.SeqTime);
        Assert.Equal(625.1, result.ParTime);
        Assert.Equal(4.0, result.Acceleration);
        Assert.Equal(80.0, result.Efficiency);
        Assert.Equal(8.0, result.TheoreticalMax);
    }

    [Fact]
    public void SessionStats_Properties_SetAndGet()
    {
        var now = DateTime.Now;
        var stats = new SessionStats
        {
            TaskSize = 1000,
            SequentialTimeMs = 5000,
            ParallelTimeMs = 1250,
            Acceleration = 4.0,
            Efficiency = 80.0,
            Timestamp = now,
            ArrivalRate = 1.5,
            ServiceRate = 2.0,
            ServiceTimeMs = 1250,
            NodeLoadPercent = 75.5,
            WorkersCount = 4,
            Residual = 1e-12
        };

        Assert.Equal(1000, stats.TaskSize);
        Assert.Equal(5000, stats.SequentialTimeMs);
        Assert.Equal(1250, stats.ParallelTimeMs);
        Assert.Equal(4.0, stats.Acceleration);
        Assert.Equal(80.0, stats.Efficiency);
        Assert.Equal(now, stats.Timestamp);
        Assert.Equal(1.5, stats.ArrivalRate);
        Assert.Equal(2.0, stats.ServiceRate);
        Assert.Equal(1250, stats.ServiceTimeMs);
        Assert.Equal(75.5, stats.NodeLoadPercent);
        Assert.Equal(4, stats.WorkersCount);
        Assert.Equal(1e-12, stats.Residual);
    }

    [Fact]
    public void SolveTask_Properties_SetAndGet()
    {
        var task = new SolveTask
        {
            MatrixPath = @"C:\matrix.txt",
            VectorPath = @"C:\vector.txt",
            Size = 500,
            Name = "TestMatrix"
        };

        Assert.Equal(@"C:\matrix.txt", task.MatrixPath);
        Assert.Equal(@"C:\vector.txt", task.VectorPath);
        Assert.Equal(500, task.Size);
        Assert.Equal("TestMatrix", task.Name);
    }

    [Fact]
    public void WorkerBenchmark_Properties_SetAndGet()
    {
        var benchmark = new WorkerBenchmark
        {
            WorkerCount = 8,
            TimeMs = 1234.56,
            MatrixSize = 2000,
            MatrixName = "LargeMatrix"
        };

        Assert.Equal(8, benchmark.WorkerCount);
        Assert.Equal(1234.56, benchmark.TimeMs);
        Assert.Equal(2000, benchmark.MatrixSize);
        Assert.Equal("LargeMatrix", benchmark.MatrixName);
    }

    [Fact]
    public void SparseMatrix_WithZeroSize_InitializesEmptyRows()
    {
        var matrix = new SparseMatrix(0);

        Assert.Equal(0, matrix.Size);
        Assert.NotNull(matrix.Rows);
        Assert.Empty(matrix.Rows);
    }

    [Fact]
    public void SparseMatrix_AddElements_StoresCorrectly()
    {
        var matrix = new SparseMatrix(3);

        matrix.Rows[0].Add(new MatrixElement { Column = 0, Value = 1 });
        matrix.Rows[0].Add(new MatrixElement { Column = 2, Value = 3 });
        matrix.Rows[1].Add(new MatrixElement { Column = 1, Value = 5 });

        Assert.Equal(2, matrix.Rows[0].Count);
        Assert.Single(matrix.Rows[1]);
        Assert.Empty(matrix.Rows[2]);
        Assert.Equal(1, matrix.Rows[0][0].Value);
        Assert.Equal(3, matrix.Rows[0][1].Value);
        Assert.Equal(5, matrix.Rows[1][0].Value);
    }

    [Fact]
    public void SessionStats_DefaultValues_AreDefault()
    {
        var stats = new SessionStats();

        Assert.Equal(0, stats.TaskSize);
        Assert.Equal(0, stats.SequentialTimeMs);
        Assert.Equal(0, stats.ParallelTimeMs);
        Assert.Equal(0, stats.Acceleration);
        Assert.Equal(0, stats.Efficiency);
        Assert.Equal(default, stats.Timestamp);
        Assert.Equal(0, stats.ArrivalRate);
        Assert.Equal(0, stats.ServiceRate);
        Assert.Equal(0, stats.ServiceTimeMs);
        Assert.Equal(0, stats.NodeLoadPercent);
        Assert.Equal(0, stats.WorkersCount);
        Assert.Equal(0, stats.Residual);
    }
}