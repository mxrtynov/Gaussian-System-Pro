using Gaussian.Shared;
using Gaussian.Solver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Gaussian.Coordinator
{
    public class SolutionCoordinator
    {
        private List<Process> _workerProcesses = new List<Process>();
        private int _nextPort = 9000;

        public SolveResponse HandleRequest(SolveRequest request, int workersCount)
        {
            var response = new SolveResponse();

            try
            {
                double[,] matrix;
                double[] b;
                LoadDenseMatrix(request.MatrixPath, request.FreeTermsPath, out matrix, out b);

                int n = matrix.GetLength(0);
                response.Size = n;

                var engine = new GaussEngine();
                double seqTime;
                var sequentialSolution = engine.SolveSequential(matrix, b, out seqTime);
                response.SequentialTimeMs = seqTime;

                if (n < 200 || workersCount <= 1)
                {
                    response.Solution = sequentialSolution;
                    response.ParallelTimeMs = seqTime;
                    response.Acceleration = 1.0;
                    response.Efficiency = 100.0;
                    response.Residual = ComputeResidual(matrix, b, sequentialSolution);
                    response.Log = "OK";
                    return response;
                }

                int basePort = _nextPort;
                _nextPort += workersCount + 5;

                for (int i = 0; i < workersCount; i++)
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "Gaussian.Worker.exe",
                        Arguments = $"{basePort + i} {n} {i} {workersCount}",
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    var process = Process.Start(psi);
                    _workerProcesses.Add(process);
                }

                Thread.Sleep(2000);

                var clients = new List<TcpClient>();
                var writers = new List<BinaryWriter>();
                var readers = new List<BinaryReader>();

                for (int i = 0; i < workersCount; i++)
                {
                    var client = new TcpClient();
                    int retry = 0;
                    while (retry < 10)
                    {
                        try
                        {
                            client.Connect("127.0.0.1", basePort + i);
                            break;
                        }
                        catch
                        {
                            retry++;
                            Thread.Sleep(500);
                            if (retry >= 10) throw;
                        }
                    }
                    clients.Add(client);
                    writers.Add(new BinaryWriter(client.GetStream()));
                    readers.Add(new BinaryReader(client.GetStream()));
                }

                for (int w = 0; w < workersCount; w++)
                {
                    for (int row = w; row < n; row += workersCount)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            writers[w].Write(matrix[row, j]);
                        }
                        writers[w].Write(b[row]);
                    }
                    writers[w].Flush();
                }

                var parallelSw = Stopwatch.StartNew();

                double[] pivotRow = new double[n];
                double[] bCopy = (double[])b.Clone();
                double[,] aCopy = (double[,])matrix.Clone();

                for (int k = 0; k < n; k++)
                {
                    int pivot = k;
                    double maxV = Math.Abs(aCopy[k, k]);
                    for (int i = k + 1; i < n; i++)
                    {
                        double v = Math.Abs(aCopy[i, k]);
                        if (v > maxV)
                        {
                            maxV = v;
                            pivot = i;
                        }
                    }

                    if (pivot != k)
                    {
                        for (int j = k; j < n; j++)
                        {
                            double temp = aCopy[k, j];
                            aCopy[k, j] = aCopy[pivot, j];
                            aCopy[pivot, j] = temp;
                        }
                        double tb = bCopy[k];
                        bCopy[k] = bCopy[pivot];
                        bCopy[pivot] = tb;
                    }

                    for (int j = 0; j < n; j++)
                    {
                        pivotRow[j] = aCopy[k, j];
                    }
                    double pivotB = bCopy[k];

                    for (int w = 0; w < workersCount; w++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            writers[w].Write(pivotRow[j]);
                        }
                        writers[w].Write(pivotB);
                        writers[w].Write(k);
                        writers[w].Flush();
                    }

                    for (int w = 0; w < workersCount; w++)
                    {
                        readers[w].ReadBoolean();
                    }

                    for (int i = k + 1; i < n; i++)
                    {
                        double factor = aCopy[i, k] / pivotRow[k];
                        if (Math.Abs(factor) > 1e-18)
                        {
                            for (int j = k; j < n; j++)
                            {
                                aCopy[i, j] -= factor * pivotRow[j];
                            }
                            bCopy[i] -= factor * pivotB;
                        }
                    }
                }

                double[] parallelSolution = new double[n];

                for (int i = n - 1; i >= 0; i--)
                {
                    int owner = i % workersCount;
                    int idx = readers[owner].ReadInt32();
                    double val = readers[owner].ReadDouble();
                    parallelSolution[idx] = val;

                    for (int w = 0; w < workersCount; w++)
                    {
                        if (w != owner)
                        {
                            writers[w].Write(idx);
                            writers[w].Write(val);
                            writers[w].Flush();
                        }
                    }
                }

                for (int w = 0; w < workersCount; w++)
                {
                    readers[w].ReadInt32();
                }

                parallelSw.Stop();
                response.ParallelTimeMs = parallelSw.Elapsed.TotalMilliseconds;
                response.Solution = parallelSolution;

                if (response.ParallelTimeMs >= response.SequentialTimeMs)
                {
                    response.Acceleration = 1.0;
                    response.Efficiency = (1.0 / workersCount) * 100;
                }
                else
                {
                    response.Acceleration = response.SequentialTimeMs / response.ParallelTimeMs;
                    response.Efficiency = (response.Acceleration / workersCount) * 100;
                }

                response.TheoreticalMax = workersCount;
                response.Cores = Environment.ProcessorCount;
                response.Residual = ComputeResidual(matrix, b, parallelSolution);

                foreach (var w in writers) w.Close();
                foreach (var r in readers) r.Close();
                foreach (var c in clients) c.Close();

                foreach (var p in _workerProcesses)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }
                }
                _workerProcesses.Clear();

                response.Log = "OK";
            }
            catch (Exception ex)
            {
                response.Log = $"ERROR: {ex.Message}";

                foreach (var p in _workerProcesses)
                {
                    try { if (!p.HasExited) p.Kill(); } catch { }
                }
                _workerProcesses.Clear();
            }

            return response;
        }

        private void LoadDenseMatrix(string matrixPath, string vectorPath, out double[,] matrix, out double[] b)
        {
            var bLines = File.ReadAllLines(vectorPath);
            int n = bLines.Length;
            b = new double[n];

            for (int i = 0; i < n; i++)
            {
                b[i] = double.Parse(bLines[i].Trim(), CultureInfo.InvariantCulture);
            }

            matrix = new double[n, n];
            int row = 0;

            foreach (var line in File.ReadLines(matrixPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int col = 0; col < parts.Length; col++)
                {
                    matrix[row, col] = double.Parse(parts[col], CultureInfo.InvariantCulture);
                }
                row++;
            }
        }

        private double ComputeResidual(double[,] matrix, double[] b, double[] x)
        {
            int n = matrix.GetLength(0);
            double maxResidual = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    sum += matrix[i, j] * x[j];
                }
                double diff = Math.Abs(sum - b[i]);
                if (diff > maxResidual) maxResidual = diff;
            }
            return maxResidual;
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
                double diff = Math.Abs(sum - b[i]);
                if (diff > maxResidual) maxResidual = diff;
            }
            return maxResidual;
        }

        public void Shutdown()
        {
            foreach (var p in _workerProcesses)
            {
                try
                {
                    if (!p.HasExited) p.Kill();
                    p.WaitForExit(1000);
                }
                catch { }
            }
            _workerProcesses.Clear();
        }
    }
}