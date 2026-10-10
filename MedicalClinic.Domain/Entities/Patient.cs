using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Domain.Entities
{
    public class Patient
    {
        public int Id { get; set; }

        // هذا المعرف رح نربطه لاحقاً بمستخدم الـ Identity عشان نعرف مين هو اليوزر بالسيستم
        public string IdentityUserId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    }
}