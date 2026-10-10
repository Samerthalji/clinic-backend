using MedicalClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
   public interface IPatientRepository:IGenericRepository<Patient>
    {
        Task<Patient> GetPatientByPhoneNumberAsync(string phoneNumber);
      
        Task<IEnumerable<Patient>> GetPatientsByNameAsync(string name);
        Task<Patient?> GetByIdentityUserIdAsync(string identityUserId);
        Task<IEnumerable<Patient>> GetPagedPatientsAsync(int pageNumber, int pageSize);
        Task<int> GetTotalCountAsync();
    }
}
