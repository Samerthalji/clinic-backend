using MedicalClinic.Application.Interfaces;
using MedicalClinic.Application.Services;
using MedicalClinic.Domain.Jwt;
using MedicalClinic.Domain.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace MedicalClinic.API.Extensions
{
    // MedicalClinic.API/Extensions/ServiceCollectionExtensions.cs
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IDoctorService, DoctorServices>();
            services.AddScoped<IPatientServices, PatientServices>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            return services;
        }
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration config)
        {
            var jwtOption = config.GetSection("JwtOptions").Get<JwtOption>()!;
            services.AddSingleton(jwtOption);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(op =>
            {
                op.SaveToken = true;
                op.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOption.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOption.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOption.Key)),
                    RoleClaimType = ClaimTypes.Role
                };
            });

            return services;
        }
    }
}
