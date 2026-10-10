using MedicalClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
   public interface IDoctorRepository:IGenericRepository<Doctor>
    {
        Task<IEnumerable<Doctor>> GetDoctorBySpecializationAsync(string specialization);
        Task<Doctor?> GetByIdentityUserIdAsync(string identityUserId);

    }
}
