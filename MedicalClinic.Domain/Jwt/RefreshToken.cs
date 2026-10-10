using MedicalClinic.Domain.Entities;

using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
namespace MedicalClinic.Domain.Jwt
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
        public DateTime CreatedOn { get; set; }
        public DateTime? RevokedOn { get; set; }
        public bool IsActive => RevokedOn == null && !IsExpired;

        // ربط الـ Refresh Token مع جدول المستخدمين (IdentityUser)
        public string UserId { get; set; } = string.Empty;
      public ApplicationUser User { get; set; } = null!;
    }
}
