using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.AuthrResponseDto
{// نموذج الاستجابة بعد تسجيل الدخول (Authentication Response Model)
    public class AuthModelDto
    {
        public string Message { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiration { get; set; }
    }
}
