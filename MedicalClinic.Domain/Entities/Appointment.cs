using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Domain.Entities
{
    public class Appointment
    {
        public int Id { get; set; }

        // ربط الموعد مع المريض والدكتور
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        public DateTime AppointmentDate { get; set; } // وقت الموعد
        public string Status { get; set; } = "Pending"; // حالة الموعد: Pending, Confirmed, Cancelled, Completed
        public string Notes { get; set; } = string.Empty; // ملاحظات أولية للموعد
    }
}

