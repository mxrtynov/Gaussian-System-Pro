using System;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Gaussian.Worker
{
    class Worker
    {
        static int Main(string[] args)
        {
            try
            {
                int port = int.Parse(args[0]);
                int n = int.Parse(args[1]);
                int rank = int.Parse(args[2]);
                int totalWorkers = int.Parse(args[3]);

                Console.WriteLine($"[{rank}] ЗАПУСК: порт={port}, размер={n}, воркеров={totalWorkers}");
                Console.Out.Flush();

                int rowsPerWorker = (n + totalWorkers - 1) / totalWorkers;
                int[] assignedRows = new int[rowsPerWorker];
                int count = 0;
                for (int i = rank; i < n; i += totalWorkers)
                {
                    assignedRows[count++] = i;
                }

                Console.WriteLine($"[{rank}] Назначено строк: {rowsPerWorker}, первые строки: {string.Join(",", assignedRows.Length > 5 ? assignedRows[..5] : assignedRows)}...");
                Console.Out.Flush();

                var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                Console.WriteLine($"[{rank}] Ожидание подключения на порту {port}...");
                Console.Out.Flush();

                var client = listener.AcceptTcpClient();
                var stream = client.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                Console.WriteLine($"[{rank}] Подключен к координатору");
                Console.Out.Flush();

                double[][] localRows = new double[rowsPerWorker][];
                double[] localB = new double[rowsPerWorker];

                Console.WriteLine($"[{rank}] Прием локальных данных...");
                Console.Out.Flush();

                for (int idx = 0; idx < rowsPerWorker; idx++)
                {
                    localRows[idx] = new double[n];
                    for (int j = 0; j < n; j++)
                    {
                        localRows[idx][j] = reader.ReadDouble();
                    }
                    localB[idx] = reader.ReadDouble();

                    if (idx % Math.Max(1, rowsPerWorker / 10) == 0 || idx == rowsPerWorker - 1)
                    {
                        Console.WriteLine($"[{rank}] Принята строка {idx + 1}/{rowsPerWorker} (глобальная строка {assignedRows[idx]})");
                        Console.Out.Flush();
                    }
                }

                Console.WriteLine($"[{rank}] Начало прямого хода (исключение Гаусса)...");
                Console.Out.Flush();

                for (int k = 0; k < n; k++)
                {
                    double[] pivotRow = new double[n];
                    double pivotB = 0;
                    int pivotRowIndex = -1;

                    for (int j = 0; j < n; j++)
                    {
                        pivotRow[j] = reader.ReadDouble();
                    }
                    pivotB = reader.ReadDouble();
                    pivotRowIndex = reader.ReadInt32();

                    if (k % Math.Max(1, n / 20) == 0 || k == n - 1)
                    {
                        Console.WriteLine($"[{rank}] Прямой ход: итерация {k + 1}/{n}, pivot строка={pivotRowIndex}");
                        Console.Out.Flush();
                    }

                    int eliminationCount = 0;
                    for (int idx = 0; idx < rowsPerWorker; idx++)
                    {
                        if (assignedRows[idx] > pivotRowIndex)
                        {
                            double factor = localRows[idx][k] / pivotRow[k];
                            if (Math.Abs(factor) > 1e-18)
                            {
                                for (int j = k; j < n; j++)
                                {
                                    localRows[idx][j] -= factor * pivotRow[j];
                                }
                                localB[idx] -= factor * pivotB;
                                eliminationCount++;
                            }
                        }
                    }

                    writer.Write(true);
                    writer.Flush();

                    if (eliminationCount > 0 && k % Math.Max(1, n / 20) == 0)
                    {
                        Console.WriteLine($"[{rank}]   исключено строк: {eliminationCount}");
                        Console.Out.Flush();
                    }
                }

                Console.WriteLine($"[{rank}] Прямой ход завершен. Начало обратного хода...");
                Console.Out.Flush();

                double[] solution = new double[n];

                for (int i = n - 1; i >= 0; i--)
                {
                    bool ownsRow = false;
                    int localIdx = -1;
                    for (int idx = 0; idx < rowsPerWorker; idx++)
                    {
                        if (assignedRows[idx] == i)
                        {
                            ownsRow = true;
                            localIdx = idx;
                            break;
                        }
                    }

                    if (ownsRow)
                    {
                        double sum = 0;
                        for (int j = i + 1; j < n; j++)
                        {
                            sum += localRows[localIdx][j] * solution[j];
                        }
                        solution[i] = (localB[localIdx] - sum) / localRows[localIdx][i];

                        writer.Write(i);
                        writer.Write(solution[i]);
                        writer.Flush();

                        if (i % Math.Max(1, n / 20) == 0 || i == 0)
                        {
                            Console.WriteLine($"[{rank}] Обратный ход: вычислено x[{i}] = {solution[i]:F6}");
                            Console.Out.Flush();
                        }
                    }
                    else
                    {
                        int idx = reader.ReadInt32();
                        double val = reader.ReadDouble();
                        solution[idx] = val;
                    }
                }

                writer.Write(-1);
                writer.Flush();

                Console.WriteLine($"[{rank}] Решение получено. Завершение работы...");
                Console.Out.Flush();

                reader.Close();
                writer.Close();
                stream.Close();
                client.Close();
                listener.Stop();

                Console.WriteLine($"[{rank}] Воркер успешно завершен");
                Console.Out.Flush();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.Out.Flush();
                return 1;
            }
        }
    }
}