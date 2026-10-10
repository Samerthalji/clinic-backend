using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MedicalClinic.Application.Dtos.PatientResponseDto
{
   public class CreatePatientDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Address { get; set; }

        // إذا حابب تستقبل الـ IdentityUserId لو مش مربوط أوتوماتيك مع التسجيل
        public string IdentityUserId { get; set; }
    }
}
