using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Gaussian.Shared;
using Gaussian.Coordinator;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Controls;

namespace Gaussian.Client
{
    public partial class MainWindow : Window
    {
        private readonly SolutionCoordinator _coordinator = new();
        private readonly DataGenerator _generator = new();
        private List<SolveTask> _batchQueue = new List<SolveTask>();
        private List<SolveTask> _testMatrices = new List<SolveTask>();
        private string _singleMatrixPath = "";
        private string _singleVectorPath = "";
        private const string TargetDir = @"D:\RIS_COURSE_PROJECT\Matrices";
        private List<SessionStats> _currentBatchSessions = new List<SessionStats>();
        private List<WorkerBenchmark> _workerBenchmarks = new List<WorkerBenchmark>();

        public MainWindow()
        {
            InitializeComponent();
            StatusCores.Text = $"CPU: {Environment.ProcessorCount} ядер";
            if (!Directory.Exists(TargetDir)) Directory.CreateDirectory(TargetDir);
            Loaded += MainWindow_Loaded;
            BtnSolve.IsEnabled = false;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ChartCanvas.SizeChanged += (s, ev) => ChartRenderer.DrawComparisonChart(ChartCanvas, _currentBatchSessions);
            NodeLoadCanvas.SizeChanged += (s, ev) => ChartRenderer.DrawNodeLoadChart(NodeLoadCanvas, _currentBatchSessions);
            WorkerChartCanvas.SizeChanged += (s, ev) => ChartRenderer.DrawWorkerChart(WorkerChartCanvas, _workerBenchmarks);
        }

        private bool IsMatrixFile(string filePath)
        {
            try
            {
                var lines = File.ReadLines(filePath).Take(5).ToList();
                if (lines.Count == 0) return false;
                int colCount = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                int rowCount = 0;
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != colCount) return false;
                    rowCount++;
                }
                return rowCount >= 2 && colCount >= 2;
            }
            catch { return false; }
        }

        private bool IsVectorFile(string filePath)
        {
            try
            {
                var lines = File.ReadLines(filePath).Take(10).ToList();
                if (lines.Count == 0) return false;
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 1) return false;
                    if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return false;
                }
                return true;
            }
            catch { return false; }
        }

        private int GetMatrixSizeFromFile(string filePath)
        {
            try
            {
                var firstLine = File.ReadLines(filePath).FirstOrDefault();
                if (firstLine == null) return 0;
                return firstLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
            catch { return 0; }
        }

        private void UpdateLoadedMatricesList()
        {
            Dispatcher.InvokeAsync(() =>
            {
                TxtLoadedMatrices.Clear();
                TxtLoadedMatrices.AppendText($"Загружено матриц: {_testMatrices.Count}\n");
                TxtLoadedMatrices.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                foreach (var task in _testMatrices)
                {
                    TxtLoadedMatrices.AppendText($"{task.Name} ({task.Size}x{task.Size})\n");
                    TxtLoadedMatrices.AppendText($"   {Path.GetFileName(task.MatrixPath)}\n");
                    TxtLoadedMatrices.AppendText($"   {Path.GetFileName(task.VectorPath)}\n");
                    TxtLoadedMatrices.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                }
            });
        }

        private void BtnGenBatch_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtGenSize.Text, out int n) || !int.TryParse(TxtGenCount.Text, out int count)) return;
            double density = SliderDensity.Value / 100.0;
            TxtLog.AppendText($"\n[ГЕНЕРАЦИЯ] Создание {count} систем (плотность: {SliderDensity.Value:F0}%)...\n");

            for (int k = 0; k < count; k++)
            {
                int currentSize = n + (k * 10);
                var (matrix, vector) = _generator.GenerateRandomSystem(currentSize, density);

                string mPath = Path.Combine(TargetDir, $"matrix_{currentSize}.txt");
                string vPath = Path.Combine(TargetDir, $"freeterms_{currentSize}.txt");

                StringBuilder sbM = new StringBuilder();
                for (int i = 0; i < currentSize; i++)
                {
                    double[] row = new double[currentSize];
                    foreach (var el in matrix.Rows[i]) row[el.Column] = el.Value;
                    sbM.AppendLine(string.Join(" ", row.Select(x => x.ToString("F6", CultureInfo.InvariantCulture))));
                }
                File.WriteAllText(mPath, sbM.ToString());
                File.WriteAllLines(vPath, vector.Select(x => x.ToString("F6", CultureInfo.InvariantCulture)));

                var newTask = new SolveTask
                {
                    MatrixPath = mPath,
                    VectorPath = vPath,
                    Size = currentSize,
                    Name = $"matrix_{currentSize}"
                };
                _batchQueue.Add(newTask);
                _testMatrices.Add(newTask);
                TxtLog.AppendText($"Добавлено: {currentSize}x{currentSize}\n");
            }

            TxtLog.AppendText($"--- Всего в очереди: {_batchQueue.Count} ---\n");
            TxtLog.ScrollToEnd();
            UpdateLoadedMatricesList();
            BtnSolve.IsEnabled = _testMatrices.Count > 0;
        }

        private void SelectMatrix_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "txt|*.txt", InitialDirectory = TargetDir };
            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FileName;
                if (!IsMatrixFile(selectedPath))
                {
                    MessageBox.Show("Ошибка: Выбранный файл не является матрицей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _singleMatrixPath = selectedPath;
                TxtLog.AppendText($"\n[ЗАГРУЗКА] Матрица: {Path.GetFileName(_singleMatrixPath)}\n");

                if (!string.IsNullOrEmpty(_singleVectorPath))
                {
                    int matrixSize = GetMatrixSizeFromFile(_singleMatrixPath);
                    int vectorSize = File.ReadLines(_singleVectorPath).Count();
                    if (matrixSize != vectorSize)
                    {
                        MessageBox.Show($"Ошибка: Размер матрицы ({matrixSize}) не совпадает с размером вектора ({vectorSize})", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        _singleMatrixPath = "";
                        _singleVectorPath = "";
                        return;
                    }
                    _testMatrices.Clear();
                    _testMatrices.Add(new SolveTask { MatrixPath = _singleMatrixPath, VectorPath = _singleVectorPath, Size = matrixSize, Name = Path.GetFileNameWithoutExtension(_singleMatrixPath) });
                    UpdateLoadedMatricesList();
                    BtnSolve.IsEnabled = true;
                }
            }
        }

        private void SelectVector_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "txt|*.txt", InitialDirectory = TargetDir };
            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FileName;
                if (!IsVectorFile(selectedPath))
                {
                    MessageBox.Show("Ошибка: Выбранный файл не является вектором.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _singleVectorPath = selectedPath;
                TxtLog.AppendText($"\n[ЗАГРУЗКА] Вектор: {Path.GetFileName(_singleVectorPath)}\n");

                if (!string.IsNullOrEmpty(_singleMatrixPath))
                {
                    int matrixSize = GetMatrixSizeFromFile(_singleMatrixPath);
                    int vectorSize = File.ReadLines(_singleVectorPath).Count();
                    if (matrixSize != vectorSize)
                    {
                        MessageBox.Show($"Ошибка: Размер матрицы ({matrixSize}) не совпадает с размером вектора ({vectorSize})", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        _singleMatrixPath = "";
                        _singleVectorPath = "";
                        return;
                    }
                    _testMatrices.Clear();
                    _testMatrices.Add(new SolveTask { MatrixPath = _singleMatrixPath, VectorPath = _singleVectorPath, Size = matrixSize, Name = Path.GetFileNameWithoutExtension(_singleMatrixPath) });
                    UpdateLoadedMatricesList();
                    BtnSolve.IsEnabled = true;
                }
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                var files = Directory.GetFiles(dialog.FolderName, "*.txt");
                var matrices = files.Where(f => Path.GetFileName(f).StartsWith("matrix_")).ToList();

                foreach (var m in matrices)
                {
                    var match = Regex.Match(Path.GetFileName(m), @"matrix_(\d+)");
                    if (match.Success)
                    {
                        string s = match.Groups[1].Value;
                        var v = files.FirstOrDefault(f => Path.GetFileName(f).Contains($"freeterms_{s}"));
                        if (v != null)
                        {
                            var newTask = new SolveTask { MatrixPath = m, VectorPath = v, Size = int.Parse(s), Name = Path.GetFileNameWithoutExtension(m) };
                            _batchQueue.Add(newTask);
                            _testMatrices.Add(newTask);
                            TxtLog.AppendText($"\nДобавлена матрица {newTask.Size}x{newTask.Size}");
                        }
                    }
                }
                TxtLog.AppendText($"\n[ПАПКА] Добавлено задач: {_batchQueue.Count}\n");
                TxtLog.ScrollToEnd();
                UpdateLoadedMatricesList();
                BtnSolve.IsEnabled = _testMatrices.Count > 0;
            }
        }

        private async void BtnSolve_Click(object sender, RoutedEventArgs e)
        {
            List<SolveTask> tasksToRun = _testMatrices.Count > 0 ? new List<SolveTask>(_testMatrices) : new List<SolveTask>(_batchQueue);
            if (tasksToRun.Count == 0) return;

            TxtLog.Clear();
            TxtLog.AppendText("═══════════════════════════════════════════════════════════════════════\n");
            TxtLog.AppendText("                         ЗАПУСК РАСЧЕТА\n");
            TxtLog.AppendText("═══════════════════════════════════════════════════════════════════════\n\n");

            int workers = int.Parse(((ComboBoxItem)ComboWorkers.SelectedItem).Content.ToString());
            BtnSolve.IsEnabled = false;
            ProgressIndicator.Visibility = Visibility.Visible;
            _currentBatchSessions.Clear();

            var completedResults = new List<(SolveTask Task, SolveResponse Response)>();

            for (int idx = 0; idx < tasksToRun.Count; idx++)
            {
                var task = tasksToRun[idx];
                TxtLog.AppendText($"\n>>> ОБРАБОТКА: {task.Name} ({task.Size}x{task.Size})...\n");

                var res = await Task.Run(() => _coordinator.HandleRequest(new SolveRequest { MatrixPath = task.MatrixPath, FreeTermsPath = task.VectorPath }, workers));

                completedResults.Add((task, res));

                double nodeLoadPercent = (res.ParallelTimeMs / Math.Max(1, res.SequentialTimeMs)) * 100;
                if (nodeLoadPercent > 100) nodeLoadPercent = 100;

                var session = new SessionStats
                {
                    TaskSize = task.Size,
                    SequentialTimeMs = res.SequentialTimeMs,
                    ParallelTimeMs = res.ParallelTimeMs,
                    Acceleration = res.Acceleration,
                    Efficiency = res.Efficiency,
                    Timestamp = DateTime.Now,
                    ServiceTimeMs = res.ParallelTimeMs,
                    NodeLoadPercent = nodeLoadPercent,
                    WorkersCount = workers,
                    Residual = res.Residual
                };
                _currentBatchSessions.Add(session);

                TxtLog.AppendText($"\n[РЕЗУЛЬТАТЫ]");
                TxtLog.AppendText($"\n    Невязка: {res.Residual:E3}");
                TxtLog.AppendText($"\n    Время последовательно: {res.SequentialTimeMs / 1000:F3} с");
                TxtLog.AppendText($"\n    Время параллельно: {res.ParallelTimeMs / 1000:F3} с");
                TxtLog.AppendText($"\n    Ускорение: {res.Acceleration:F2}x");
                TxtLog.AppendText($"\n    Эффективность: {res.Efficiency:F1}%");
                TxtLog.AppendText("\n------------------------------------------------------------\n");
                TxtLog.ScrollToEnd();

                await Dispatcher.InvokeAsync(() => UpdateAnalyticsTab());
            }

            TxtLog.AppendText("\n═══════════════════════════════════════════════════════════════════════\n");
            TxtLog.AppendText("                       РАСЧЕТ ЗАВЕРШЕН\n");
            TxtLog.AppendText("═══════════════════════════════════════════════════════════════════════\n");

            await Dispatcher.InvokeAsync(async () =>
            {
                foreach (var (task, res) in completedResults)
                {
                    if (string.IsNullOrEmpty(res.Log) || !res.Log.Contains("ERROR"))
                    {
                        var result = MessageBox.Show(
                            $"Сохранить результаты решения для матрицы {task.Name} в файл?",
                            "Сохранение результатов",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            SaveSolutionToFile(res, task.Name);
                        }
                    }
                }
            });

            BtnSolve.IsEnabled = true;
            ProgressIndicator.Visibility = Visibility.Collapsed;
        }

        private void SaveSolutionToFile(SolveResponse response, string matrixName)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = $"Сохранить решение для {matrixName}",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"solution_{matrixName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {

                        if (response.Solution != null)
                        {
                            for (int i = 0; i < response.Solution.Length; i++)
                            {
                                writer.WriteLine(response.Solution[i]);
                            }
                        }
                    }

                    TxtLog.AppendText($"\n[СОХРАНЕНИЕ] Результаты для {matrixName} сохранены в файл: {Path.GetFileName(saveFileDialog.FileName)}\n");
                    TxtLog.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnRunWorkerTest_Click(object sender, RoutedEventArgs e)
        {
            List<SolveTask> tasksToTest = _testMatrices.Count > 0 ? new List<SolveTask>(_testMatrices) : new List<SolveTask>(_batchQueue);
            if (tasksToTest.Count == 0)
            {
                TxtLog.AppendText("\n[ОШИБКА] Нет загруженных матриц для тестирования\n");
                return;
            }

            TxtLog.Clear();
            TxtLog.AppendText("═══════════════════════════════════════════════════════════════════════\n");
            TxtLog.AppendText("                     ТЕСТ ПРОИЗВОДИТЕЛЬНОСТИ\n");
            TxtLog.AppendText("═══════════════════════════════════════════════════════════════════════\n\n");

            BtnRunWorkerTest.IsEnabled = false;
            ProgressIndicator.Visibility = Visibility.Visible;
            _workerBenchmarks.Clear();

            int[] workerCounts = new int[] { 1, 2, 4, 6, 8 };

            foreach (var task in tasksToTest)
            {
                TxtLog.AppendText($"\n[ТЕСТ] Матрица: {task.Name} ({task.Size}x{task.Size})\n");

                foreach (int workers in workerCounts)
                {
                    TxtLog.AppendText($"  Тест с {workers} узлами... ");
                    var res = await Task.Run(() => _coordinator.HandleRequest(new SolveRequest { MatrixPath = task.MatrixPath, FreeTermsPath = task.VectorPath }, workers));

                    _workerBenchmarks.Add(new WorkerBenchmark
                    {
                        WorkerCount = workers,
                        TimeMs = res.ParallelTimeMs,
                        MatrixSize = task.Size,
                        MatrixName = task.Name
                    });

                    TxtLog.AppendText($"готово ({res.ParallelTimeMs / 1000:F2} с)\n");
                    await Dispatcher.InvokeAsync(() => ChartRenderer.DrawWorkerChart(WorkerChartCanvas, _workerBenchmarks));
                    await Task.Delay(50);
                }
            }

            TxtLog.AppendText($"\n[ТЕСТ ЗАВЕРШЕН]\n");
            BtnRunWorkerTest.IsEnabled = true;
            ProgressIndicator.Visibility = Visibility.Collapsed;
        }

        private void UpdateAnalyticsTab()
        {
            if (_currentBatchSessions.Count == 0) return;

            double avgSequentialTime = _currentBatchSessions.Average(s => s.SequentialTimeMs);
            double avgParallelTime = _currentBatchSessions.Average(s => s.ParallelTimeMs);
            double avgAcceleration = _currentBatchSessions.Average(s => s.Acceleration);
            double avgNodeLoad = _currentBatchSessions.Average(s => s.NodeLoadPercent);

            double avgServiceTime = _currentBatchSessions.Average(s => s.ServiceTimeMs);

            double totalTime = _currentBatchSessions.Sum(s => s.ServiceTimeMs) / 1000.0;
            double arrivalRate = _currentBatchSessions.Count / Math.Max(0.001, totalTime);
            double serviceRate = 1000.0 / Math.Max(0.001, avgServiceTime);

            Dispatcher.InvokeAsync(() =>
            {
                TxtAvgSeqTime.Text = $"{avgSequentialTime / 1000:F2} с";
                TxtAvgParTime.Text = $"{avgParallelTime / 1000:F2} с";
                TxtAvgAccel.Text = $"{avgAcceleration:F2}x";
                TxtAvgNodeLoad.Text = $"{avgNodeLoad:F1}%";
                NodeLoadProgress.Value = avgNodeLoad;

                TxtAvgServiceTime.Text = $"{avgServiceTime:F0} мс";
                TxtArrivalRate.Text = $"{arrivalRate:F3}";
                TxtServiceRate.Text = $"{serviceRate:F3}";
            });

            ChartRenderer.DrawComparisonChart(ChartCanvas, _currentBatchSessions);
            ChartRenderer.DrawNodeLoadChart(NodeLoadCanvas, _currentBatchSessions);
        }

        private void SliderDensity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int roundedValue = (int)Math.Round(e.NewValue);
            LblDensityValue.Text = $"{roundedValue}%";
            SliderDensity.Value = roundedValue;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var p in Process.GetProcessesByName("Gaussian.Worker"))
                try { p.Kill(); } catch { }
            _coordinator.Shutdown();
        }
    }
}