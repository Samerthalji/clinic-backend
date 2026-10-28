using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Application.Services;
using MedicalClinic.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicalClinic.API.Tests
{
    public class AppointmentServiceTests
    {

        private const string UserId = "user-1";
        private static readonly DateTime Future = DateTime.UtcNow.AddDays(1);
        private static readonly DateTime Past = DateTime.UtcNow.AddDays(-1);

        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAppointmentRepository> _appointments = new();
        private readonly Mock<IPatientRepository> _patients = new();
        private readonly Mock<IDoctorRepository> _doctors = new();
        private readonly AppointmentService _service; // the REAL class 


        public AppointmentServiceTests()
        {
            _uow.Setup(u => u.Appointments).Returns(_appointments.Object);
            _uow.Setup(u => u.Patients).Returns(_patients.Object);
            _uow.Setup(u => u.Doctors).Returns(_doctors.Object);
            _service = new AppointmentService(_uow.Object);   
        }

        // ---------------- helpers ----------------

        // the logged-in account "user-1" is patient number patientId
        private void SetupPatientUser(int patientId) =>
            _patients.Setup(p => p.GetByIdentityUserIdAsync(UserId))
                     .ReturnsAsync(new Patient { Id = patientId, FirstName = "Sara", LastName = "Omar" });

        // the logged-in account "user-1" is doctor number doctorId
        private void SetupDoctorUser(int doctorId) =>
            _doctors.Setup(d => d.GetByIdentityUserIdAsync(UserId))
                    .ReturnsAsync(new Doctor { Id = doctorId, FirstName = "Ali", LastName = "Saleh" });

        // the account "user-1" has no doctor profile
        private void SetupNoDoctorUser() =>
            _doctors.Setup(d => d.GetByIdentityUserIdAsync(UserId)).ReturnsAsync((Doctor?)null);

        private void SetupNoPatientUser() =>
            _patients.Setup(p => p.GetByIdentityUserIdAsync(UserId)).ReturnsAsync((Patient?)null);

        private void SetupActiveDoctor(int doctorId) =>
            _doctors.Setup(d => d.GetByIdAsync(doctorId))
                    .ReturnsAsync(new Doctor { Id = doctorId, FirstName = "Ali", LastName = "Saleh", IsActive = true });

        // appointment returned by GetByIdAsync (no related entities)
        private Appointment SetupAppointment(int id, int patientId, int doctorId, string status = "Pending")
        {
            var appointment = new Appointment
            {
                Id = id,
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = Future,
                Status = status,
                Notes = ""
            };
            _appointments.Setup(a => a.GetByIdAsync(id)).ReturnsAsync(appointment);
            return appointment;
        }

        // appointment that already has Doctor and Patient loaded (like the Include in the repository)
        private static Appointment FullAppointment(int id, int patientId, int doctorId) => new()
        {
            Id = id,
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = Future,
            Status = "Pending",
            Doctor = new Doctor { Id = doctorId, FirstName = "Ali", LastName = "Saleh" },
            Patient = new Patient { Id = patientId, FirstName = "Sara", LastName = "Omar" }
        };
        // =====================================================================
        // CreateAsync(currentUserId, dto): 5 exits, one test for each
        // =====================================================================

        [Fact]
        public async Task CreateAsync_WhenNoPatientProfile_ThrowsBadRequestException()
        {
            SetupNoPatientUser();

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateAsync(UserId, new CreateAppointmentDto { DoctorId = 1, AppointmentDate = Future }));
        }

        [Fact]
        public async Task CreateAsync_WhenDoctorMissing_ThrowsNotFoundException()
        {
            SetupPatientUser(2);
            _doctors.Setup(d => d.GetByIdAsync(99)).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateAsync(UserId, new CreateAppointmentDto { DoctorId = 99, AppointmentDate = Future }));
        }

        [Fact]
        public async Task CreateAsync_WhenDoctorInactive_ThrowsNotFoundException()
        {
            SetupPatientUser(2);
            _doctors.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(new Doctor { Id = 1, IsActive = false });

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateAsync(UserId, new CreateAppointmentDto { DoctorId = 1, AppointmentDate = Future }));
        }

        [Fact]
        public async Task CreateAsync_WhenDateInPast_ThrowsBadRequest_WithoutCheckingAvailability()
        {
            SetupPatientUser(2);
            SetupActiveDoctor(1);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateAsync(UserId, new CreateAppointmentDto { DoctorId = 1, AppointmentDate = Past }));

            _appointments.Verify(a => a.IsDoctorAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenSlotTaken_ThrowsBadRequest_AndDoesNotSave()   // double-booking
        {
            SetupPatientUser(2);
            SetupActiveDoctor(1);
            _appointments.Setup(a => a.IsDoctorAvailableAsync(1, Future)).ReturnsAsync(false);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateAsync(UserId, new CreateAppointmentDto { DoctorId = 1, AppointmentDate = Future }));

            _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>()), Times.Never);
            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEverythingValid_SavesPendingAppointment_AndReturnsDto()
        {
            SetupPatientUser(2);
            SetupActiveDoctor(1);
            _appointments.Setup(a => a.IsDoctorAvailableAsync(1, Future)).ReturnsAsync(true);

            var result = await _service.CreateAsync(UserId,
                new CreateAppointmentDto { DoctorId = 1, AppointmentDate = Future, Notes = "checkup" });

            // the patient comes from the token (patient 2), not from the request
            _appointments.Verify(a => a.AddAsync(It.Is<Appointment>(x =>
                x.PatientId == 2 && x.DoctorId == 1 && x.Status == "Pending" && x.Notes == "checkup")), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);

            Assert.Equal("Pending", result.Status);
            Assert.Equal(2, result.PatientId);
            Assert.Equal("Ali Saleh", result.DoctorFullName);
            Assert.Equal("Sara Omar", result.PatientFullName);
        }
        // =====================================================================
        // GetMyAppointmentsAsync / GetMyDoctorAppointmentsAsync
        // =====================================================================

        [Fact]
        public async Task GetMyAppointmentsAsync_WhenNoPatientProfile_ThrowsBadRequestException()
        {
            SetupNoPatientUser();

            await Assert.ThrowsAsync<BadRequestException>(() => _service.GetMyAppointmentsAsync(UserId));
        }

        [Fact]
        public async Task GetMyAppointmentsAsync_ReturnsOnlyThisPatientsAppointmentsAsDtos()
        {
            SetupPatientUser(2);
            _appointments.Setup(a => a.GetPatientsAppointmentsAsync(2))
                         .ReturnsAsync(new List<Appointment> { FullAppointment(10, 2, 1), FullAppointment(11, 2, 3) });

            var result = (await _service.GetMyAppointmentsAsync(UserId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Sara Omar", result[0].PatientFullName);
        }

        [Fact]
        public async Task GetMyDoctorAppointmentsAsync_WhenNoDoctorProfile_ThrowsBadRequestException()
        {
            SetupNoDoctorUser();

            await Assert.ThrowsAsync<BadRequestException>(() => _service.GetMyDoctorAppointmentsAsync(UserId));
        }

        [Fact]
        public async Task GetMyDoctorAppointmentsAsync_ReturnsThisDoctorsAppointmentsAsDtos()
        {
            SetupDoctorUser(1);
            _appointments.Setup(a => a.GetDoctorAppointmentsAsync(1))
                         .ReturnsAsync(new List<Appointment> { FullAppointment(10, 2, 1) });

            var result = (await _service.GetMyDoctorAppointmentsAsync(UserId)).ToList();

            Assert.Single(result);
            Assert.Equal("Ali Saleh", result[0].DoctorFullName);
        }
        // =====================================================================
        // GetByIdAsync(id, currentUserId, isAdmin)
        // =====================================================================

        [Fact]
        public async Task GetByIdAsync_WhenAppointmentMissing_ThrowsNotFoundException()
        {
            _appointments.Setup(a => a.GetByIdAsync(99)).ReturnsAsync((Appointment?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99, UserId, false));
        }

        [Fact]
        public async Task GetByIdAsync_WhenUserIsNotAPartyAndNotAdmin_ThrowsForbiddenException()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupPatientUser(99);      // logged-in patient is someone else
            SetupNoDoctorUser();

            await Assert.ThrowsAsync<ForbiddenException>(() => _service.GetByIdAsync(5, UserId, false));
        }

        [Fact]
        public async Task GetByIdAsync_WhenPatientReadsOwnAppointment_ReturnsDtoWithNames()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupPatientUser(2);       // owner
            SetupNoDoctorUser();//عشان مايكون is part =>true
            _doctors.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(new Doctor { Id = 1, FirstName = "Ali", LastName = "Saleh" });
            _patients.Setup(p => p.GetByIdAsync(2)).ReturnsAsync(new Patient { Id = 2, FirstName = "Sara", LastName = "Omar" });

            var result = await _service.GetByIdAsync(5, UserId, false);

            Assert.Equal(5, result.Id);
            Assert.Equal("Ali Saleh", result.DoctorFullName);
            Assert.Equal("Sara Omar", result.PatientFullName);
        }

        [Fact]
        public async Task GetByIdAsync_WhenAdmin_ReadsAnyAppointment()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);
            _doctors.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(new Doctor { Id = 1, FirstName = "Ali", LastName = "Saleh" });
            _patients.Setup(p => p.GetByIdAsync(2)).ReturnsAsync(new Patient { Id = 2, FirstName = "Sara", LastName = "Omar" });

            var result = await _service.GetByIdAsync(5, "admin-account", true);

            Assert.Equal(5, result.Id);
        }
        // =====================================================================
        // UpdateStatusAsync(id, statusDto, currentUserId, isAdmin, isPatient)
        // =====================================================================

        [Theory]
        [InlineData("Done")]
        [InlineData("")]
        [InlineData("cancelled-ish")]
        public async Task UpdateStatusAsync_WhenStatusInvalid_ThrowsBadRequestException(string status)
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = status }, UserId, true, false));
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenAppointmentMissing_ThrowsNotFoundException()
        {
            _appointments.Setup(a => a.GetByIdAsync(5)).ReturnsAsync((Appointment?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Confirmed" }, UserId, true, false));
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenPatientIsNotPartyOfAppointment_ThrowsForbidden_AndDoesNotSave()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupPatientUser(99);
            SetupNoDoctorUser();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Cancelled" }, UserId, false, true));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenPatientTriesToConfirm_ThrowsForbidden_AndDoesNotSave()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupPatientUser(2);       // own appointment, but a patient may only cancel
            SetupNoDoctorUser();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Confirmed" }, UserId, false, true));

            Assert.Equal("Pending", appointment.Status);
            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenPatientCancelsOwnAppointment_UpdatesStatus_AndSaves()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupPatientUser(2);
            SetupNoDoctorUser();

            await _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Cancelled" }, UserId, false, true);

            Assert.Equal("Cancelled", appointment.Status);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenAdminConfirms_UpdatesStatus_AndSaves()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);

            await _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Confirmed" }, "admin-account", true, false);

            Assert.Equal("Confirmed", appointment.Status);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenDoctorOfTheAppointmentConfirms_UpdatesStatus_AndSaves()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupDoctorUser(1);        // the appointment's doctor
            SetupNoPatientUser();

            await _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Confirmed" }, UserId, false, false);

            Assert.Equal("Confirmed", appointment.Status);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        
        [Fact]
        public async Task UpdateStatusAsync_WhenReactivatingCancelledAppointmentInTakenSlot_ThrowsBadRequest()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1, status: "Cancelled");
            _appointments.Setup(a => a.IsDoctorAvailableAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdateStatusAsync(5, new UpdateAppointmentStatusDto { Status = "Confirmed" }, "admin-account", true, false));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        // =====================================================================
        // UpdateAsync(id, dto, currentUserId, isAdmin)
        // =====================================================================

        [Fact]
        public async Task UpdateAsync_WhenAppointmentMissing_ThrowsNotFoundException()
        {
            _appointments.Setup(a => a.GetByIdAsync(5)).ReturnsAsync((Appointment?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateAsync(5, new UpdateAppointmentDto { AppointmentDate = Future }, UserId, true));
        }

        [Fact]
        public async Task UpdateAsync_WhenUserIsNotAPartyAndNotAdmin_ThrowsForbidden_AndDoesNotSave()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);
            SetupPatientUser(99);
            SetupNoDoctorUser();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.UpdateAsync(5, new UpdateAppointmentDto { AppointmentDate = Future }, UserId, false));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNewDateInPast_ThrowsBadRequest_AndDoesNotSave()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdateAsync(5, new UpdateAppointmentDto { AppointmentDate = Past }, "admin-account", true));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNewDateAndDoctorBusy_ThrowsBadRequest_AndDoesNotSave()
        {
            SetupAppointment(5, patientId: 2, doctorId: 1);              // currently on "Future"
            var newDate = Future.AddHours(3);
            _appointments.Setup(a => a.IsDoctorAvailableAsync(1, newDate)).ReturnsAsync(false);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdateAsync(5, new UpdateAppointmentDto { AppointmentDate = newDate }, "admin-account", true));

            _appointments.Verify(a => a.Update(It.IsAny<Appointment>()), Times.Never);
            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNewDateAndDoctorFree_UpdatesAppointment_AndSaves()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);
            var newDate = Future.AddHours(3);
            _appointments.Setup(a => a.IsDoctorAvailableAsync(1, newDate)).ReturnsAsync(true);

            await _service.UpdateAsync(5, new UpdateAppointmentDto { AppointmentDate = newDate, Notes = "moved" },
                                       "admin-account", true);

            Assert.Equal(newDate, appointment.AppointmentDate);
            Assert.Equal("moved", appointment.Notes);
            _appointments.Verify(a => a.Update(appointment), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenOnlyNotesChange_DoesNotCheckAvailability()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);   // date stays "Future"

            await _service.UpdateAsync(5, new UpdateAppointmentDto { AppointmentDate = Future, Notes = "new notes" },
                                       "admin-account", true);

            Assert.Equal("new notes", appointment.Notes);
            _appointments.Verify(a => a.IsDoctorAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }
        // =====================================================================
        // DeleteAsync(id)
        // =====================================================================

        [Fact]
        public async Task DeleteAsync_WhenAppointmentMissing_ThrowsNotFoundException_AndDoesNotSave()
        {
            _appointments.Setup(a => a.GetByIdAsync(99)).ReturnsAsync((Appointment?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenAppointmentExists_DeletesAndSaves()
        {
            var appointment = SetupAppointment(5, patientId: 2, doctorId: 1);

            await _service.DeleteAsync(5);

            _appointments.Verify(a => a.Delete(appointment), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

    }

}
