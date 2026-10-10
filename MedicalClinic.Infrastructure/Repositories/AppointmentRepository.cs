using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using MedicalClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Infrastructure.Repositories
{

    public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
    {
        public  AppDbContext _context;
        public AppointmentRepository(AppDbContext context): base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId)
        {
            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId).Include(a=>a.Patient).Include(a => a.Doctor)
                .ToListAsync();

        }

        public async Task<IEnumerable<Appointment>> GetPatientsAppointmentsAsync(int patientId)
        {
            return await _context.Appointments
                .Where(a => a.PatientId == patientId).Include(a=>a.Patient).Include(a => a.Doctor)
                .ToListAsync();
        }
       

        public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate)
        {
            return !await _context.Appointments.AnyAsync(a =>
      a.DoctorId == doctorId &&
      a.AppointmentDate == appointmentDate &&
      a.Status != "Cancelled");
        }


        // Implement methods for appointment-specific queries here
    }
}
