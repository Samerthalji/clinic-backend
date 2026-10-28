using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.PatientDto
{
    public class PagedResultDto<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
    }
}
