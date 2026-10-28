using MedicalClinic.Application.Dtos.PatientDto;
using MedicalClinic.Application.Dtos.PatientResponseDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
    public interface IPatientServices
    {
        Task<PagedResultDto<PatientDto>> GetAllAsync(int pageNumber, int pageSize);
        Task<PatientDto> GetMeAsync(string CurrentUserId);
        Task<PatientDto> GetByIdAsync(int id,string CurrentUserId,bool IsStaff);
        Task<IEnumerable<PatientDto>> SearchByNameAsync(string Name);
        Task UpdateAsync(int id, UpdatePatientDto dto, string currentUserId, bool isAdmin);
        Task DeleteAsync(int id);
    }
}
