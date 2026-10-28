using MedicalClinic.Application.Dtos.DoctorResponseDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Services
{
    public class DoctorServices : IDoctorService
    {
        private readonly IUnitOfWork _unitOfWork;
        public DoctorServices(IUnitOfWork UnitOfWork)
        {
            _unitOfWork = UnitOfWork;
        }
        private static DoctorDto ToDto(Doctor d) => new()
        {
            Id = d.Id,
            FirstName = d.FirstName,
            LastName = d.LastName,
            Specialization = d.Specialization,
            ClinicRoom = d.ClinicRoom,
            Email = d.Email,
            PhoneNumber = d.PhoneNumber,
            ConsultationFee = d.ConsultationFee,
            IsActive = d.IsActive

        };
        public async Task DeleteDoctorAsync(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id)
                         ?? throw new NotFoundException("الدكتور غير موجود");
            _unitOfWork.Doctors.Delete(doctor);
            await _unitOfWork.CompleteAsync();


        }

        public async Task<IEnumerable<DoctorDto>> GetallDoctorAsync()
        {
            var doctor = await _unitOfWork.Doctors.GetAllAsync();
            return doctor.Select(ToDto).ToList();
        }

        public async Task<DoctorDto> GetDoctorByIdAsync(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id)
                ?? throw new NotFoundException("الدكتور غير موجود");
            return ToDto(doctor);

        }

        public async Task<IEnumerable<DoctorDto>> GetDoctorBySpecializationAsync(string specialization)
        {
            var doctors = await _unitOfWork.Doctors.GetDoctorBySpecializationAsync(specialization);
            return doctors.Select(ToDto).ToList();
        }

      

       

        public async Task ToggleDoctorActiveAsync(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id)
                       ?? throw new NotFoundException("الدكتور غير موجود");
            doctor.IsActive = !doctor.IsActive;
            _unitOfWork.Doctors.Update(doctor);
             await _unitOfWork.CompleteAsync();
           

        }

       
        public async Task UpdateDoctorAsync(int id, UpdateDoctorDto dto)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id)
                ?? throw new NotFoundException("الدكتور غير موجود");

            doctor.FirstName = dto.FirstName;
            doctor.LastName = dto.LastName;
            doctor.Specialization = dto.Specialization;
            doctor.ClinicRoom = dto.ClinicRoom;
            doctor.Email = dto.Email;
            doctor.PhoneNumber = dto.PhoneNumber;
            doctor.ConsultationFee = dto.ConsultationFee;
            doctor.IsActive = dto.IsActive;

            _unitOfWork.Doctors.Update(doctor);
            await _unitOfWork.CompleteAsync();
        }

       
    }
}
