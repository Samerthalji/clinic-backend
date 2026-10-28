using MedicalClinic.Application.Dtos.DoctorResponseDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
  public interface IDoctorService
    {
        Task<IEnumerable<DoctorDto>> GetallDoctorAsync();
        Task<DoctorDto> GetDoctorByIdAsync(int id);
        Task<IEnumerable<DoctorDto>>GetDoctorBySpecializationAsync(string specialization);
        Task UpdateDoctorAsync(int id, UpdateDoctorDto dto);
        Task DeleteDoctorAsync(int id);
        Task ToggleDoctorActiveAsync(int id);
    }
}
