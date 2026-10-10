using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Domain.Jwt
{
    public class JwtOption
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public float Lifetime { get; set; }
        public string Key { get; set; } = string.Empty;
    }
}