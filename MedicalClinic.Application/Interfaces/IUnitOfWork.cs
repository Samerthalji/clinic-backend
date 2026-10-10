using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.Application.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        IPatientRepository Patients { get; }
        // هنا سنقوم لاحقاً بإضافة الـ Repositories الخاصة بكل كيان
        // مثال: IPatientRepository Patients { get; }
        IDoctorRepository Doctors { get; }
        IAppointmentRepository Appointments { get; }
        Task<int> CompleteAsync();
    }
}
