using MedicalClinic.Application.Interfaces;
using MedicalClinic.Infrastructure.Data;
using MedicalClinic.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;



namespace MedicalClinic.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IPatientRepository Patients  {get;private set;}
        public IDoctorRepository Doctors { get; private set; }
        public IAppointmentRepository Appointments { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
           Patients = new PatientRepository(_context);
           Doctors = new DoctorRepository(_context);
           Appointments = new AppointmentRepository(_context);
    }



        public Task<int> CompleteAsync()
        {
            return  _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
