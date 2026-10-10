using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MedicalClinic.Application.Dtos.DoctorResponseDto
{
    public class UpdateDoctorDto
    {
        [Required]
        public int Id { get; set; } // لمعرفة أي دكتور سنقوم بتعديله

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Specialization { get; set; } = string.Empty;

        [Required]
        public string ClinicRoom { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Range(0, 10000)]
        public decimal ConsultationFee { get; set; }

        public bool IsActive { get; set; } // للسماح بتفعيل أو تعطيل الطبيب
    }
}
