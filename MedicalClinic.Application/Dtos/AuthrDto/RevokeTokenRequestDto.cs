using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.AuthrResponseDto
{
    public class RevokeTokenRequestDto
    {
        public string refreshToken { get; set; } = string.Empty;
    }
}
