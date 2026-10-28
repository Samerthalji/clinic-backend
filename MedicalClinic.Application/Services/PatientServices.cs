using MedicalClinic.Application.Dtos.PatientDto;
using MedicalClinic.Application.Dtos.PatientResponseDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Services
{
   public class PatientServices:IPatientServices
    {
        private readonly IUnitOfWork _unitOfWork;
        public PatientServices(IUnitOfWork UnitofWork)
        {
            _unitOfWork = UnitofWork;
        }
        private static PatientDto ToDto(Patient p) => new()
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            PhoneNumber = p.PhoneNumber,
            DateOfBirth = p.DateOfBirth,
            Address = p.Address



        };

        public async Task DeleteAsync(int id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id)
                ?? throw new NotFoundException("المريض غير موجود");
            _unitOfWork.Patients.Delete(patient);
             await _unitOfWork.CompleteAsync();
        }

        public async Task<PagedResultDto<PatientDto>> GetAllAsync(int pageNumber, int pageSize)
        { 
            if (pageNumber< 1) pageNumber = 1;
            if (pageSize< 1) pageSize = 10;
        
            var paients = await _unitOfWork.Patients.GetPagedPatientsAsync(pageNumber,pageSize);
            var totalcount = await _unitOfWork.Patients.GetTotalCountAsync();
            return new PagedResultDto<PatientDto>()
            {
                TotalCount = totalcount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = paients.Select(ToDto).ToList()


            };
           
        }

        public async Task<PatientDto> GetByIdAsync(int id, string CurrentUserId, bool IsStaff)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id)
                ?? throw new NotFoundException("المريض غير موجود");
            if(!IsStaff&&CurrentUserId != patient.IdentityUserId)
            {
                throw new ForbiddenException("لا يمكن الوصول للمريض اذا لم تكن ادمن او دكتور");
            }
            return (ToDto(patient));
        }

        public async Task<PatientDto> GetMeAsync(string CurrentUserId)
        {
            var patientprofile = await _unitOfWork.Patients.GetByIdentityUserIdAsync(CurrentUserId)
                ?? throw new NotFoundException("المريض غيرموجود");
            return (ToDto(patientprofile));
        }

        public async Task<IEnumerable<PatientDto>> SearchByNameAsync(string Name)
        {
            var patients = await _unitOfWork.Patients.GetPatientsByNameAsync(Name);
                if (patients == null || !patients.Any())
            {
                throw new NotFoundException("No patients found with the given name");
            }

            return patients.Select(ToDto).ToList();
        }

        public async Task UpdateAsync(int id, UpdatePatientDto dto, string currentUserId, bool isAdmin)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id)
                ?? throw new NotFoundException("المريض غير موجود");
            if (patient.IdentityUserId != currentUserId && !isAdmin)
            {
                throw new ForbiddenException("غير مسمحلك التعديل على المريض");
            }
            patient.FirstName = dto.FirstName;
            patient.LastName = dto.LastName;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.DateOfBirth = dto.DateOfBirth;
            patient.Address = dto.Address;
            _unitOfWork.Patients.Update(patient);
            await _unitOfWork.CompleteAsync();
            
        }
    

    }
}
