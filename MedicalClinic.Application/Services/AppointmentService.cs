using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Services
{
    public class AppointmentService:IAppointmentService
    {

        private static readonly string[] AllowedStatuses = { "Pending", "Confirmed", "Cancelled", "Completed" };

        private readonly IUnitOfWork _unitOfWork;

        public AppointmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        private static AppointmentDto ToDto(Appointment a) => new()
        {
            Id = a.Id,
            DoctorId = a.DoctorId,
            DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
            PatientId = a.PatientId,
            PatientFullName = $"{a.Patient.FirstName} {a.Patient.LastName}",
            AppointmentDate = a.AppointmentDate,
            Status = a.Status,
            Notes = a.Notes
        };
        private async Task<bool> IsPartyAsync(Appointment appointment, string currentUserId)
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(currentUserId);
            var doctor = await _unitOfWork.Doctors.GetByIdentityUserIdAsync(currentUserId);
            return (patient != null && appointment.PatientId == patient.Id)
                || (doctor != null && appointment.DoctorId == doctor.Id);
        }
        public async Task<AppointmentDto> CreateAsync(string CurrentUserId,CreateAppointmentDto cdto)
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(CurrentUserId)
                ?? throw new BadRequestException("لا يوجد صفحة لهذا المريض");
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(cdto.DoctorId);
               if (doctor == null || !doctor.IsActive)
                   throw new NotFoundException("الدكتور غير موجود");

               if(cdto.AppointmentDate <= DateTime.UtcNow)
            {
                throw new BadRequestException("لا يمكن الحجز بتاريخ بالماضي");
            }

            if (!await _unitOfWork.Appointments.IsDoctorAvailableAsync(cdto.DoctorId, cdto.AppointmentDate))
                throw new BadRequestException("الدكتور غير متوفر في هذا الوقت");
            var Appointment= new Appointment
            {
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                AppointmentDate = cdto.AppointmentDate,
                Notes = cdto.Notes,
                Status = "Pending"

            };
            await _unitOfWork.Appointments.AddAsync(Appointment);
            await _unitOfWork.CompleteAsync();
            Appointment.Doctor = doctor;
            Appointment.Patient = patient;
            return ToDto(Appointment);

        }

        public async Task<IEnumerable<AppointmentDto>> GetMyAppointmentsAsync(string currentUserId)
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(currentUserId)
                ??throw new BadRequestException("ما في بروفايل مريض لهذا الحساب");

            var appointments = await _unitOfWork.Appointments.GetPatientsAppointmentsAsync(patient.Id);
            return appointments.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<AppointmentDto>> GetMyDoctorAppointmentsAsync(string currentUserId)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdentityUserIdAsync(currentUserId)
               ?? throw new BadRequestException("ما في بروفايل دكتور لهذا الحساب");

            var appointments = await _unitOfWork.Appointments.GetDoctorAppointmentsAsync(doctor.Id);
            return appointments.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<AppointmentDto>> GetDoctorAppointmentsAsync(int doctorId)
        {
            var appointments = await _unitOfWork.Appointments.GetDoctorAppointmentsAsync(doctorId);
            return appointments.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<AppointmentDto>> GetPatientAppointmentsAsync(int patientId)
        {
            var appointments = await _unitOfWork.Appointments.GetPatientsAppointmentsAsync(patientId);
            return appointments.Select(ToDto).ToList();
        }
        public async Task<AppointmentDto> GetByIdAsync(int id, string currentUserId, bool isAdmin)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                throw new NotFoundException("الموعد غير موجود");

            if (!isAdmin && !await IsPartyAsync(appointment, currentUserId))
                throw new ForbiddenException("ليس لديك الصلاحية للوصول للموعد");

            // GetByIdAsync does not load the related entities, so load them and attach them before mapping.
            appointment.Doctor = await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId)
                ?? throw new NotFoundException("الدكتور غير موجود");
            appointment.Patient = await _unitOfWork.Patients.GetByIdAsync(appointment.PatientId)
                ?? throw new NotFoundException("المريض غير موجود");

            return ToDto(appointment);
        }

        public async Task UpdateStatusAsync(int id, UpdateAppointmentStatusDto statusDto, string currentUserId, bool isAdmin, bool isPatient)
        {
            if (!AllowedStatuses.Contains(statusDto.Status))
                throw new BadRequestException("حالة غير صحيحة");

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                throw new NotFoundException("الموعد غير موجود");

            if (!isAdmin && !await IsPartyAsync(appointment, currentUserId))
                throw new ForbiddenException("لا يوجد لديك صلاحية لتعديل الموعد");

            // A patient (not admin) may only cancel
            if (!isAdmin && isPatient && statusDto.Status != "Cancelled")
                throw new ForbiddenException("يمكن للمريض الغاء الموعد فقط");
            if (appointment.Status == "Cancelled" && statusDto.Status != "Cancelled")
            {
                bool isDoctorAvailable = await _unitOfWork.Appointments.IsDoctorAvailableAsync(appointment.DoctorId, appointment.AppointmentDate);

                if (!isDoctorAvailable)
                {
                    throw new BadRequestException("عذراً، لا يمكن تفعيل الموعد لأن وقت الدكتور أصبح محجوزاً لموعد آخر");
                }
            }

            appointment.Status = statusDto.Status;
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateAsync(int id, UpdateAppointmentDto dto, string currentUserId, bool isAdmin)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id)
                ?? throw new NotFoundException("الموعد غير موجود");

            if (!isAdmin && !await IsPartyAsync(appointment, currentUserId))
                throw new ForbiddenException("ليس لديك صلاحية لتعديل هذا الموعد");

            if (dto.AppointmentDate <= DateTime.UtcNow)
                throw new BadRequestException("لا يمكن التعديل لتاريخ بالماضي");

            if (appointment.AppointmentDate != dto.AppointmentDate)
            {
                var isAvailable = await _unitOfWork.Appointments.IsDoctorAvailableAsync(appointment.DoctorId, dto.AppointmentDate);
                if (!isAvailable)
                    throw new BadRequestException("الدكتور غير متوفر في هذا الوقت");
            }

            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.Notes = dto.Notes;
            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id)
                ?? throw new NotFoundException("الموعد غير موجود");

            _unitOfWork.Appointments.Delete(appointment);
            await _unitOfWork.CompleteAsync();
        }



    }
}
