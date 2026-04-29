using System;
using System.Collections.Generic;
using Gaussian.Shared;

namespace Gaussian.Coordinator
{
    public class DataGenerator
    {
        private readonly Random _random = new Random();

        public (SparseMatrix matrix, double[] vector) GenerateRandomSystem(int size, double density)
        {
            SparseMatrix matrix = GenerateRandomMatrix(size, density);
            double[] exactSolution = GenerateRandomSolution(size);
            double[] b = MultiplyMatrixByVector(matrix, exactSolution);
            return (matrix, b);
        }

        private SparseMatrix GenerateRandomMatrix(int size, double density)
        {
            SparseMatrix matrix = new SparseMatrix(size);

            for (int i = 0; i < size; i++)
            {
                double sumAbsOffDiagonal = 0;

                for (int j = 0; j < size; j++)
                {
                    if (i != j && _random.NextDouble() < density)
                    {
                        double value = (_random.NextDouble() * 2) - 1;
                        matrix.Rows[i].Add(new MatrixElement { Column = j, Value = value });
                        sumAbsOffDiagonal += Math.Abs(value);
                    }
                }

                double diagonal = sumAbsOffDiagonal + (_random.NextDouble() * 10) + 1;
                matrix.Rows[i].Add(new MatrixElement { Column = i, Value = diagonal });
            }

            return matrix;
        }

        private double[] GenerateRandomSolution(int size)
        {
            double[] solution = new double[size];
            for (int i = 0; i < size; i++)
            {
                solution[i] = (_random.NextDouble() * 20) - 10;
            }
            return solution;
        }

        private double[] MultiplyMatrixByVector(SparseMatrix matrix, double[] vector)
        {
            int n = matrix.Size;
            double[] result = new double[n];

            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                foreach (var element in matrix.Rows[i])
                {
                    sum += element.Value * vector[element.Column];
                }
                result[i] = sum;
            }

            return result;
        }
    }
}