using MedicalClinic.API.Extensions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using MedicalClinic.Infrastructure;
using MedicalClinic.Infrastructure.Data;
using MedicalClinic.Infrastructure.Services;
using MedicalClinic.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MedicalClinic.API.ExceptionHandling;

namespace MedicalClinic.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // DbContext
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        // Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // JWT (لازم بعد AddIdentity عشان يغلب الـ default schemes)
        builder.Services.AddJwtAuthentication(builder.Configuration);

        // Services
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IAuthService, AuthServices>();
       
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        //
        builder.Services.AddCors(o => o.AddPolicy("front", p =>
        p.WithOrigins("http://localhost:5173")
        .AllowAnyHeader().AllowAnyMethod()));
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            ApplicationDbContextSeed.SeedRolesAsync(roleManager).GetAwaiter().GetResult();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            ApplicationDbContextSeed.SeedAdminAsync(
                userManager,
                config["SeedAdmin:Email"],
                config["SeedAdmin:Password"]).GetAwaiter().GetResult();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
        }
        app.UseHttpsRedirection();
        app.UseCors("front");
        app.UseExceptionHandler(_ => { });

        app.UseAuthentication();   // قبل UseAuthorization
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}