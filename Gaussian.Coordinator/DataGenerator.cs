using System;
using Gaussian.Shared;

namespace Gaussian.Coordinator
{
    public class DataGenerator
    {
        private Random _random = new Random();

        public (SparseMatrix, double[]) GenerateRandomSystem(int size, double density)
        {
            var matrix = new SparseMatrix(size);
            var vector = new double[size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (_random.NextDouble() < density || i == j)
                    {
                        double value = (_random.NextDouble() * 200) - 100;
                        if (Math.Abs(value) > 1e-14)
                        {
                            matrix.Rows[i].Add(new MatrixElement { Column = j, Value = value });
                        }
                    }
                }
                vector[i] = (_random.NextDouble() * 200) - 100;
            }

            return (matrix, vector);
        }
    }
}