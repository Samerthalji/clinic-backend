using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
    public interface IAppointmentService
    {
        // Patient books an appointment for himself (identity comes from the token, never from the request body)
        Task<AppointmentDto> CreateAsync(string currentUserId, CreateAppointmentDto dto);

        // Reads
        //مريض بيجيب مواعيده
        Task<IEnumerable<AppointmentDto>> GetMyAppointmentsAsync(string currentUserId);
        //دكتور بيجيب مواعيده
        Task<IEnumerable<AppointmentDto>> GetMyDoctorAppointmentsAsync(string currentUserId);
        //ادمن بيجيب مواعيد الدكتور والمريض
        Task<IEnumerable<AppointmentDto>> GetDoctorAppointmentsAsync(int doctorId);
        Task<IEnumerable<AppointmentDto>> GetPatientAppointmentsAsync(int patientId);
        //ادمن او المريض نفسه
        Task<AppointmentDto> GetByIdAsync(int id, string currentUserId, bool isAdmin);

        // Writes
        //ادمن او المريض  او طرف بالموعد
        Task UpdateStatusAsync(int id, UpdateAppointmentStatusDto statusDto, string currentUserId, bool isAdmin, bool isPatient);
        //ادمن او طرف بالموعد
        Task UpdateAsync(int id, UpdateAppointmentDto dto, string currentUserId, bool isAdmin);
        //ادمن فقط
        Task DeleteAsync(int id);
    }
}
