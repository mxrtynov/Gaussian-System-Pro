using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Gaussian.Shared;

namespace Gaussian.Client
{
    public static class ChartRenderer
    {
        private static Color[] _matrixColors = new Color[]
        {
            Color.FromRgb(67, 97, 238),
            Color.FromRgb(16, 185, 129),
            Color.FromRgb(245, 158, 11),
            Color.FromRgb(239, 68, 68),
            Color.FromRgb(139, 92, 246),
            Color.FromRgb(236, 72, 153),
            Color.FromRgb(14, 165, 233),
            Color.FromRgb(168, 85, 247),
            Color.FromRgb(234, 179, 8),
            Color.FromRgb(34, 197, 94)
        };

        public static void DrawComparisonChart(Canvas canvas, List<SessionStats> sessions)
        {
            if (sessions == null || sessions.Count == 0 || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

            canvas.Children.Clear();

            double maxTime = Math.Max(sessions.Max(s => s.SequentialTimeMs), sessions.Max(s => s.ParallelTimeMs));
            if (maxTime <= 0) maxTime = 1;

            double chartWidth = canvas.ActualWidth - 120;
            double chartHeight = canvas.ActualHeight - 100;
            if (chartWidth <= 0 || chartHeight <= 0) return;

            DrawGridLines(canvas, chartWidth, chartHeight, maxTime);

            double barWidth = Math.Min(30, chartWidth / (sessions.Count * 3));
            double startX = 70;
            double bottomY = canvas.ActualHeight - 70;

            for (int i = 0; i < sessions.Count; i++)
            {
                Color color = _matrixColors[i % _matrixColors.Length];
                double x = startX + i * (barWidth * 2.5);
                double seqHeight = (sessions[i].SequentialTimeMs / maxTime) * chartHeight;
                double parHeight = (sessions[i].ParallelTimeMs / maxTime) * chartHeight;

                Color seqColor = Color.FromRgb((byte)(color.R * 0.6), (byte)(color.G * 0.6), (byte)(color.B * 0.6));

                if (seqHeight > 0)
                {
                    Rectangle seqBar = new Rectangle { Width = barWidth, Height = seqHeight, Fill = new SolidColorBrush(seqColor) };
                    Canvas.SetTop(seqBar, bottomY - seqHeight);
                    Canvas.SetLeft(seqBar, x);
                    canvas.Children.Add(seqBar);
                }

                if (parHeight > 0)
                {
                    Rectangle parBar = new Rectangle { Width = barWidth, Height = parHeight, Fill = new SolidColorBrush(color) };
                    Canvas.SetTop(parBar, bottomY - parHeight);
                    Canvas.SetLeft(parBar, x + barWidth + 5);
                    canvas.Children.Add(parBar);
                }

                TextBlock label = new TextBlock
                {
                    Text = sessions[i].TaskSize.ToString(),
                    FontSize = 9,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240))
                };
                Canvas.SetTop(label, bottomY + 5);
                Canvas.SetLeft(label, x + barWidth / 2);
                canvas.Children.Add(label);
            }

            AddComparisonLegend(canvas);
        }

        public static void DrawNodeLoadChart(Canvas canvas, List<SessionStats> sessions)
        {
            if (sessions == null || sessions.Count == 0 || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

            canvas.Children.Clear();

            double chartWidth = canvas.ActualWidth - 100;
            double chartHeight = canvas.ActualHeight - 70;
            if (chartWidth <= 0 || chartHeight <= 0) return;

            for (int i = 0; i <= 100; i += 20)
            {
                double y = canvas.ActualHeight - 50 - (i / 100.0) * chartHeight;
                Line gridLine = new Line
                {
                    X1 = 60,
                    Y1 = y,
                    X2 = canvas.ActualWidth - 20,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184)),
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(gridLine);

                TextBlock gridLabel = new TextBlock
                {
                    Text = $"{i}%",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
                Canvas.SetLeft(gridLabel, 10);
                Canvas.SetTop(gridLabel, y - 8);
                canvas.Children.Add(gridLabel);
            }

            double barWidth = Math.Min(40, chartWidth / (sessions.Count * 1.5));
            double startX = 70;
            double bottomY = canvas.ActualHeight - 50;

            for (int i = 0; i < sessions.Count; i++)
            {
                Color color = _matrixColors[i % _matrixColors.Length];
                double x = startX + i * (barWidth + 15);
                double loadPercent = sessions[i].NodeLoadPercent;
                double barHeight = (loadPercent / 100.0) * chartHeight;

                if (barHeight > 0)
                {
                    Rectangle loadBar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Fill = new SolidColorBrush(color)
                    };
                    Canvas.SetTop(loadBar, bottomY - barHeight);
                    Canvas.SetLeft(loadBar, x);
                    canvas.Children.Add(loadBar);

                    TextBlock percentLabel = new TextBlock
                    {
                        Text = $"{loadPercent:F0}%",
                        FontSize = 9,
                        FontWeight = System.Windows.FontWeights.Bold,
                        Foreground = new SolidColorBrush(color)
                    };
                    Canvas.SetTop(percentLabel, bottomY - barHeight - 18);
                    Canvas.SetLeft(percentLabel, x + barWidth / 4);
                    canvas.Children.Add(percentLabel);
                }

                TextBlock label = new TextBlock
                {
                    Text = sessions[i].TaskSize.ToString(),
                    FontSize = 9,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240))
                };
                Canvas.SetTop(label, bottomY + 5);
                Canvas.SetLeft(label, x + barWidth / 4);
                canvas.Children.Add(label);
            }

            TextBlock xLabel = new TextBlock
            {
                Text = "Размер матрицы",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            Canvas.SetLeft(xLabel, canvas.ActualWidth / 2 - 50);
            Canvas.SetBottom(xLabel, 10);
            canvas.Children.Add(xLabel);
        }

        public static void DrawWorkerChart(Canvas canvas, List<WorkerBenchmark> benchmarks)
        {
            if (benchmarks == null || benchmarks.Count == 0 || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

            canvas.Children.Clear();

            var groupedByMatrix = benchmarks.GroupBy(b => b.MatrixName).ToList();
            double maxTime = benchmarks.Max(b => b.TimeMs);
            if (maxTime <= 0) maxTime = 1;

            double chartWidth = canvas.ActualWidth - 120;
            double chartHeight = canvas.ActualHeight - 100;
            if (chartWidth <= 0 || chartHeight <= 0) return;

            for (int i = 0; i <= 5; i++)
            {
                double y = canvas.ActualHeight - 70 - (i * chartHeight / 5);
                double val = maxTime * i / 5 / 1000;

                Line gridLine = new Line
                {
                    X1 = 70,
                    Y1 = y,
                    X2 = canvas.ActualWidth - 30,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184)),
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(gridLine);

                TextBlock gridLabel = new TextBlock
                {
                    Text = $"{val:F1}с",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
                Canvas.SetLeft(gridLabel, 10);
                Canvas.SetTop(gridLabel, y - 8);
                canvas.Children.Add(gridLabel);
            }

            for (int mIdx = 0; mIdx < groupedByMatrix.Count; mIdx++)
            {
                var matrix = groupedByMatrix[mIdx];
                Color color = _matrixColors[mIdx % _matrixColors.Length];
                var sortedData = matrix.OrderBy(b => b.WorkerCount).ToList();

                Polyline line = new Polyline { Stroke = new SolidColorBrush(color), StrokeThickness = 2.5 };

                for (int i = 0; i < sortedData.Count; i++)
                {
                    double x = 70 + (i * chartWidth / (sortedData.Count - 1));
                    double y = canvas.ActualHeight - 70 - (sortedData[i].TimeMs / maxTime) * chartHeight;
                    line.Points.Add(new Point(x, y));

                    Ellipse point = new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1.5
                    };
                    Canvas.SetLeft(point, x - 4);
                    Canvas.SetTop(point, y - 4);
                    canvas.Children.Add(point);
                }
                canvas.Children.Add(line);
            }

            int[] workerCounts = new int[] { 1, 2, 4, 6, 8 };
            for (int i = 0; i < workerCounts.Length; i++)
            {
                double x = 70 + (i * chartWidth / (workerCounts.Length - 1));
                TextBlock workerLabel = new TextBlock
                {
                    Text = workerCounts[i].ToString(),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240))
                };
                Canvas.SetTop(workerLabel, canvas.ActualHeight - 45);
                Canvas.SetLeft(workerLabel, x - 8);
                canvas.Children.Add(workerLabel);
            }

            TextBlock xLabel = new TextBlock
            {
                Text = "Количество узлов (воркеров)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            Canvas.SetLeft(xLabel, canvas.ActualWidth / 2 - 80);
            Canvas.SetBottom(xLabel, 10);
            canvas.Children.Add(xLabel);

            TextBlock yLabel = new TextBlock
            {
                Text = "Время выполнения (секунды)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            double yLabelX = 15;
            double yLabelY = canvas.ActualHeight / 2 - 40;
            Canvas.SetLeft(yLabel, yLabelX);
            Canvas.SetTop(yLabel, yLabelY);
            RotateTransform rotate = new RotateTransform(-90, 0, yLabelY + 10);
            yLabel.RenderTransform = rotate;
            canvas.Children.Add(yLabel);

            double legendY = 15;
            for (int mIdx = 0; mIdx < Math.Min(groupedByMatrix.Count, 6); mIdx++)
            {
                var matrix = groupedByMatrix[mIdx];
                Color color = _matrixColors[mIdx % _matrixColors.Length];

                Rectangle legendRect = new Rectangle { Width = 12, Height = 12, Fill = new SolidColorBrush(color) };
                Canvas.SetTop(legendRect, legendY + (mIdx * 20));
                Canvas.SetLeft(legendRect, canvas.ActualWidth - 150);
                canvas.Children.Add(legendRect);

                TextBlock legendText = new TextBlock
                {
                    Text = matrix.Key,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225))
                };
                Canvas.SetTop(legendText, legendY + (mIdx * 20) - 2);
                Canvas.SetLeft(legendText, canvas.ActualWidth - 133);
                canvas.Children.Add(legendText);
            }
        }

        private static void DrawGridLines(Canvas canvas, double width, double height, double maxValue)
        {
            for (int i = 0; i <= 5; i++)
            {
                double y = canvas.ActualHeight - 50 - (i * height / 5);
                double val = maxValue * i / 5 / 1000;

                Line gridLine = new Line
                {
                    X1 = 60,
                    Y1 = y,
                    X2 = canvas.ActualWidth - 20,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184)),
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(gridLine);

                TextBlock gridLabel = new TextBlock
                {
                    Text = $"{val:F1}с",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
                Canvas.SetLeft(gridLabel, 10);
                Canvas.SetTop(gridLabel, y - 8);
                canvas.Children.Add(gridLabel);
            }
        }

        private static void AddComparisonLegend(Canvas canvas)
        {
            double legendX = 15;
            double legendY = canvas.ActualHeight - 20;

            TextBlock parText = new TextBlock
            {
                Text = "Последовательно (яркий)",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            Canvas.SetTop(parText, legendY - 30);
            Canvas.SetLeft(parText, legendX);
            canvas.Children.Add(parText);

            TextBlock seqText = new TextBlock
            {
                Text = "Параллельно (темный)",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            Canvas.SetTop(seqText, legendY - 14);
            Canvas.SetLeft(seqText, legendX);
            canvas.Children.Add(seqText);
        }
    }
}