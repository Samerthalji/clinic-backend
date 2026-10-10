using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.AppointmentsResponedDto
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorFullName { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientFullName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
