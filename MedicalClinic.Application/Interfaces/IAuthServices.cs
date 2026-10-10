using MedicalClinic.Application.Dtos.AuthrResponseDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthModelDto> RegisterAsync(RegisterModelDto model);
        Task<AuthModelDto> RegisterWithRoleAsync(RegisterModelDto model, string role, bool generateTokens = false);
        Task<(bool Success, string Message)> AssignRoleAsync(AssignRoleDto model);
        Task<AuthModelDto> GetTokenAsync(TokenRequestModelDto model); // هذه هي دالة تسجيل الدخول (Login)
        Task<AuthModelDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
    }
}