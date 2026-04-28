using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussian.Shared
{
    public class SparseMatrix
    {
        public int Size { get; set; }
        public List<MatrixElement>[] Rows { get; set; }

        public SparseMatrix(int size)
        {
            Size = size;
            Rows = new List<MatrixElement>[size];
            for (int i = 0; i < size; i++) Rows[i] = new List<MatrixElement>();
        }

        public SparseMatrix Clone()
        {
            var clone = new SparseMatrix(Size);
            for (int i = 0; i < Size; i++)
            {
                clone.Rows[i] = new List<MatrixElement>(Rows[i].Count);
                foreach (var item in Rows[i]) clone.Rows[i].Add(new MatrixElement { Column = item.Column, Value = item.Value });
            }
            return clone;
        }
    }
}
