using MedicalClinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
public interface IAppointmentRepository:IGenericRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId);
        Task<IEnumerable<Appointment>> GetPatientsAppointmentsAsync(int patientId);
        Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate);
    }
    







    
}
