using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.DoctorResponseDto
{
    public class DoctorDto
    {
        public int Id { get; set; } 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string ClinicRoom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public bool IsActive { get; set; }
    }
}
