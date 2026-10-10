using System;
using System.Collections.Generic;
using System.Text;
 using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using MedicalClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalClinic.Infrastructure.Repositories
{
    public class PatientRepository : GenericRepository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context) : base(context)
        {
            Context = context;
            // هنا يمكننا إضافة أي وظائف خاصة بمستودع المرضى إذا لزم الأمر
        }

        public AppDbContext Context { get; }

        public async Task<Patient> GetPatientByPhoneNumberAsync(string phoneNumber)
        {
            return  await Context.Patients.FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);
        }
     
        public async Task<IEnumerable<Patient>> GetPatientsByNameAsync(string name)
        {
            return await Context.Patients
                .Where(p => p.FirstName.ToLower().Contains(name.ToLower()) || p.LastName.ToLower().Contains(name.ToLower()))
                .ToListAsync();
        }
        public async Task<IEnumerable<Patient>> GetPagedPatientsAsync(int pageNumber, int pageSize)
        {
            return await Context.Patients
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<int> GetTotalCountAsync()
        {
            return await Context.Patients.CountAsync();
        }
        public async Task<Patient?> GetByIdentityUserIdAsync(string identityUserId) {
            return await Context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId);
        }
    }
}
