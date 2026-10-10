using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Dtos.AuthrResponseDto
{

    // نموذج طلب الرمز المميز ,ويستخدم لستجيل الدخول  (Token Request Model)
    public class TokenRequestModelDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
