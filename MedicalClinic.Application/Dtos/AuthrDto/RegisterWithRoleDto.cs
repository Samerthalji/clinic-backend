using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.AuthrResponseDto
{
    public class RegisterWithRoleDto : RegisterModelDto
    {
        public string Role { get; set; } = string.Empty;
    }

    
}
