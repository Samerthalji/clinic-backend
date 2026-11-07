using MedicalClinic.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.API.Tests.Integration
{
   public class Customwebapplicationfactory:WebApplicationFactory<Program>
    {
        public const string AdminEmail = "admin@test.com";
        public const string AdminPassword = "Admin@12345";
        private readonly SqliteConnection _connection=new("Data Source=:memory:");
        public Customwebapplicationfactory()
        {
            Environment.SetEnvironmentVariable("JwtOptions__Key", "integration-tests-secret-key-0123456789-abcdefghij");
            Environment.SetEnvironmentVariable("SeedAdmin__Email", AdminEmail);
            Environment.SetEnvironmentVariable("SeedAdmin__Password", AdminPassword);
            _connection.Open();

        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
              
                var sqlServerRegistrations = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    (d.ServiceType.IsGenericType
                     && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")
                     && d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))).ToList();
                foreach (var registration in sqlServerRegistrations)
                {
                    services.Remove(registration);
                }
               services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
                var seedOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
                using var context = new AppDbContext(seedOptions);
                context.Database.EnsureCreated();
            });
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }
    }

}
