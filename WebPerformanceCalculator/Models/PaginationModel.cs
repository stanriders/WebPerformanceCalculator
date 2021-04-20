using System;

namespace WebPerformanceCalculator.Models
{
    public class PaginationModel<T>
    {
        public int Total { get; set; }

        public int TotalNotFiltered { get; set; }

        public T[] Rows { get; set; } = Array.Empty<T>();

        public int Offset { get; set; }
    }
}
