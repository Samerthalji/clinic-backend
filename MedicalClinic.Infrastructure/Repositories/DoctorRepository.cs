using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using MedicalClinic.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
namespace MedicalClinic.Infrastructure.Repositories
{
    internal class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
    {
        public DoctorRepository(AppDbContext context):base(context)
        {
            Context= context;   
        }

        public AppDbContext Context { get; }
        public async Task<IEnumerable<Doctor>> GetDoctorBySpecializationAsync(string specialization)
        {
            return await Context.Doctors.Where(d=>d.Specialization.ToLower()== specialization.ToLower()&& d.IsActive).ToListAsync();
        }
        public async Task<Doctor?> GetByIdentityUserIdAsync(string identityUserId) {
            return await Context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == identityUserId);
        }


    }
}
