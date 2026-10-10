using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MedicalClinic.Application.Dtos.AuthrResponseDto
{// نموذج تسجيل مستخدم جديد (Register Model)
    public class RegisterModelDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        public DateTime?DateOfBirth { get; set; }
        
    }
}
