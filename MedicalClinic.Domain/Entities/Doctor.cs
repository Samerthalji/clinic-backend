using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Domain.Entities
{
    public class Doctor
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty; // لربطه بنظام الصلاحيات (ASP.NET Core Identity)
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty; 
        public string ClinicRoom { get; set; } = string.Empty; 
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; } 
        public bool IsActive { get; set; } = true; 

        // العلاقة مع المواعيد
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}