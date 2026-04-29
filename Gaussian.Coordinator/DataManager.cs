using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Gaussian.Shared;

namespace Gaussian.Coordinator
{
    public class DataManager
    {
        public (SparseMatrix, double[]) LoadFiles(string matrixPath, string vectorPath)
        {
            var bLines = File.ReadAllLines(vectorPath);
            int n = bLines.Length;
            double[] b = new double[n];

            for (int i = 0; i < n; i++)
                b[i] = double.Parse(bLines[i], CultureInfo.InvariantCulture);

            var matrix = new SparseMatrix(n);
            int rowIdx = 0;

            foreach (var line in File.ReadLines(matrixPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int colIdx = 0; colIdx < parts.Length; colIdx++)
                {
                    double val = double.Parse(parts[colIdx], CultureInfo.InvariantCulture);
                    if (Math.Abs(val) > 1e-15)
                        matrix.Rows[rowIdx].Add(new MatrixElement { Column = colIdx, Value = val });
                }
                rowIdx++;
            }

            return (matrix, b);
        }
    }
}