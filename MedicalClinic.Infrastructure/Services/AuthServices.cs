using MedicalClinic.Application.Dtos.AuthrResponseDto;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using MedicalClinic.Domain.Jwt;
using MedicalClinic.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

namespace MedicalClinic.Infrastructure.Services
{
    public class AuthServices : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtOption _jwtOptions;
        private readonly AppDbContext _appDbContext;

        public AuthServices(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, JwtOption jwtOptions, AppDbContext appDbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions;
            _appDbContext = appDbContext;
        }
        public async Task<AuthModelDto> GetTokenAsync(TokenRequestModelDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return new AuthModelDto { Message = "Invalid email or password" };
            }
            var jwtToken = await GenerateTokenAsync(user);
            var refreshToken = GenerateRefreshToken(user);
            _appDbContext.RefreshTokens.Add(refreshToken);
            _appDbContext.SaveChanges();
            var roles = await _userManager.GetRolesAsync(user);
            var authModel = new AuthModelDto
            {
                IsAuthenticated = true,
                Username = user.UserName,
                Email = user.Email,
                Roles = roles.ToList(),
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresOn = jwtToken.ValidTo,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresOn
            };
            return authModel;
        }

        public async Task<AuthModelDto> RefreshTokenAsync(string refreshToken)
        {
            var existingRefreshToken = await _appDbContext.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (existingRefreshToken == null || !existingRefreshToken.IsActive)
            {
                return new AuthModelDto { Message = "Invalid or expired refresh token" };
            }
            var user = existingRefreshToken.User;
            var jwtToken = await GenerateTokenAsync(user);
            var newRefreshToken = GenerateRefreshToken(user);
            _appDbContext.RefreshTokens.Add(newRefreshToken);
            await _appDbContext.SaveChangesAsync();
            var roles = await _userManager.GetRolesAsync(user);
            var authModel = new AuthModelDto
            {
                IsAuthenticated = true,
                Username = user.UserName,
                Email = user.Email,
                Roles = roles.ToList(),
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresOn = jwtToken.ValidTo,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiration = newRefreshToken.ExpiresOn
            };
            return authModel;
        }

        public Task<AuthModelDto> RegisterAsync(RegisterModelDto model)
            => RegisterWithRoleAsync(model, "Patient", true);
       

        public async Task<AuthModelDto> RegisterWithRoleAsync(RegisterModelDto model, string role, bool generateTokens = false)
        {
            // 1) نتأكد إن الدور موجود
            if (!await _roleManager.RoleExistsAsync(role))
                return new AuthModelDto { Message = "Role does not exist" };

            // 2) تحقق مبكر من تاريخ الميلاد (قبل إنشاء أي شي بالـ DB)
            if (role == "Patient")
            {
                if (model.DateOfBirth == null)
                    return new AuthModelDto { Message = "تاريخ الميلاد مطلوب للمريض" };

                if (model.DateOfBirth.Value > DateTime.UtcNow)
                    return new AuthModelDto { Message = "تاريخ ميلاد غير صحيح" };
            }

            // 3) نتأكد إن الإيميل والـ username مش مستخدمين
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return new AuthModelDto { Message = "Email already exists" };

            if (await _userManager.FindByNameAsync(model.Username) != null)
                return new AuthModelDto { Message = "Username already exists" };

            // 4) تعريف الـ user
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Username,
                Email = model.Email
            };

            // 5) user + role + profile داخل transaction وحدة
            await using var tx = await _appDbContext.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new AuthModelDto { Message = $"User creation failed: {errors}" };
                }

                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return new AuthModelDto { Message = $"Assigning role failed: {errors}" };
                }

                if (role == "Patient")
                {
                    _appDbContext.Patients.Add(new Patient
                    {
                        IdentityUserId = user.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        Address = string.Empty,
                        DateOfBirth = model.DateOfBirth!.Value
                    });
                }
                else if (role == "Doctor")
                {
                    _appDbContext.Doctors.Add(new Doctor
                    {
                        IdentityUserId = user.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        IsActive = true
                    });
                }

                await _appDbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            // 6) بدون tokens (الأدمن بينشئ مستخدم)
            if (!generateTokens)
            {
                return new AuthModelDto
                {
                    IsAuthenticated = true,
                    Username = user.UserName,
                    Email = user.Email,
                    Roles = new List<string> { role }
                };
            }

            // 7) مع tokens (مريض بيسجّل بنفسه)
            // 7) مع tokens (مريض بيسجّل بنفسه)
            var jwtToken = await GenerateTokenAsync(user);
            var refreshToken = GenerateRefreshToken(user);
            await _appDbContext.RefreshTokens.AddAsync(refreshToken);
            await _appDbContext.SaveChangesAsync();

            return new AuthModelDto
            {
                IsAuthenticated = true,
                Username = user.UserName,
                Email = user.Email,
                Roles = new List<string> { role },
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                ExpiresOn = jwtToken.ValidTo,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresOn
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var existingRefreshToken = _appDbContext.RefreshTokens.Include(rt => rt.User).FirstOrDefault(rt => rt.Token == refreshToken);
            if (existingRefreshToken == null || !existingRefreshToken.IsActive)
            {
                return false;
            }
            existingRefreshToken.RevokedOn = DateTime.UtcNow;
            _appDbContext.RefreshTokens.Update(existingRefreshToken);
            await _appDbContext.SaveChangesAsync();
            return true;
        }
        private async Task<System.IdentityModel.Tokens.Jwt.JwtSecurityToken> GenerateTokenAsync(ApplicationUser user)
        {
            var userclaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleclaim = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }.Union(userclaims).Union(roleclaim);
            var SymmetricSecurityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var signingCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(SymmetricSecurityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var JwtSecurityToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.Lifetime),
                signingCredentials: signingCredentials
            );

            return JwtSecurityToken;
        }
        public async Task<(bool Success, string Message)> AssignRoleAsync(AssignRoleDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return (false, "User not found");

            if (!await _roleManager.RoleExistsAsync(model.Role))
                return (false, "Role does not exist");

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count == 1 && currentRoles[0] == model.Role)
                return (false, "User already has this role");

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return (false, "Failed to remove old roles");

            var addResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
                return (false, "Failed to assign role");

            // ننشئ البروفايل المناسب إذا مش موجود (هاد الجزء الجديد)
            if (model.Role == "Doctor" &&
                !await _appDbContext.Doctors.AnyAsync(d => d.IdentityUserId == user.Id))
            {
                _appDbContext.Doctors.Add(new Doctor
                {
                    IdentityUserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    IsActive = true
                });
            }
            else if (model.Role == "Patient" &&
                !await _appDbContext.Patients.AnyAsync(p => p.IdentityUserId == user.Id))
            {
                _appDbContext.Patients.Add(new Patient
                {
                    IdentityUserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DateOfBirth = DateTime.UtcNow
                });
            }

            // نلغي الـ refresh tokens الفعّالة
            var activeTokens = await _appDbContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedOn == null && rt.ExpiresOn > DateTime.UtcNow)
                .ToListAsync();
            foreach (var t in activeTokens)
                t.RevokedOn = DateTime.UtcNow;

            await _appDbContext.SaveChangesAsync();   // بتحفظ البروفايل والـ tokens مع بعض

            return (true, $"Role changed to {model.Role}");
        }
        private RefreshToken GenerateRefreshToken(ApplicationUser user)
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiresOn = DateTime.UtcNow.AddDays(7), // صلاحية الـ Refresh Token 7 أيام
                CreatedOn = DateTime.UtcNow,
                UserId = user.Id
            };
        }
    }
}
