using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Security.Claims;

namespace MedicalClinic.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public AppointmentsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin => User.IsInRole("Admin");
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
        // هل المستخدم الحالي طرف بالموعد (مريضه أو دكتوره)؟
        private async Task<bool> IsPartyAsync(Appointment appointment)
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(CurrentUserId);
            var doctor = await _unitOfWork.Doctors.GetByIdentityUserIdAsync(CurrentUserId);
            return (patient != null && appointment.PatientId == patient.Id)
                || (doctor != null && appointment.DoctorId == doctor.Id);
        }
       
        [Authorize(Roles = "Patient")]
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(CurrentUserId);
            if (patient == null)
                return BadRequest(new { message = "لا يوجد صفحة لهذا المريض" });

            var doctor = await _unitOfWork.Doctors.GetByIdAsync(dto.DoctorId);
            if (doctor == null || !doctor.IsActive)
                return NotFound(new { message = "الدكتور غير موجود" });

            if (dto.AppointmentDate <= DateTime.UtcNow)
                return BadRequest(new { message = "لا يمكن الحجز بتاريخ بالماضي" });

            if (!await _unitOfWork.Appointments.IsDoctorAvailableAsync(dto.DoctorId, dto.AppointmentDate))
                return BadRequest(new { message = "الدكتور غير متوفر في هذا الوقت" });

            var appointment = new Appointment
            {
                DoctorId = dto.DoctorId,
                PatientId = patient.Id,
                AppointmentDate = dto.AppointmentDate,
                Notes = dto.Notes,
                Status = "Pending"
            };
            await _unitOfWork.Appointments.AddAsync(appointment);
            await _unitOfWork.CompleteAsync();

            appointment.Doctor = doctor;
            appointment.Patient = patient;
            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, ToDto(appointment));
        }
        // المريض يشوف مواعيده
        [Authorize(Roles = "Patient")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(CurrentUserId);
            if (patient == null)
                return BadRequest(new { message = "ما في بروفايل مريض لهذا الحساب" });

            var appointments = await _unitOfWork.Appointments.GetPatientsAppointmentsAsync(patient.Id);
            var appointmentDtos = appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                PatientId = a.PatientId,
                PatientFullName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Notes = a.Notes
            }).ToList();
            return Ok(appointmentDtos);
        }

        // الدكتور يشوف مواعيده
        [Authorize(Roles = "Doctor")]
        [HttpGet("doctor/my")]
        public async Task<IActionResult> GetMyDoctorAppointments()
        {
            var doctor = await _unitOfWork.Doctors.GetByIdentityUserIdAsync(CurrentUserId);
            if (doctor == null)
                return BadRequest(new { message = "ما في بروفايل دكتور لهذا الحساب" });

            var appointments = await _unitOfWork.Appointments.GetDoctorAppointmentsAsync(doctor.Id);
            var appointmentDtos = appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                PatientId = a.PatientId,
                PatientFullName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Notes = a.Notes
            }).ToList();
            return Ok(appointmentDtos);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("doctor/{doctorId:int}")]
        public async Task<IActionResult> GetDoctorAppointments(int doctorId)
        {
            var appointment = await _unitOfWork.Appointments.GetDoctorAppointmentsAsync(doctorId);
            var appointmentdto = appointment.Select(a => new AppointmentDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                PatientId = a.PatientId,
                PatientFullName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Notes = a.Notes
            }).ToList();
            return Ok(appointmentdto);

        }
        [Authorize(Roles = "Admin")]
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetPatientAppointments(int patientId)
        {
            var appointments = await _unitOfWork.Appointments.GetPatientsAppointmentsAsync(patientId);

            var appointmentDtos = appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                DoctorId = a.DoctorId,
                DoctorFullName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                PatientId = a.PatientId,
                PatientFullName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Notes = a.Notes
            }).ToList();

            return Ok(appointmentDtos);
        }
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusDto statusDto)
        {
            var allowed = new[] { "Pending", "Confirmed", "Cancelled", "Completed" };
            if (!allowed.Contains(statusDto.Status))
                return BadRequest(new { message = "حالة غير صحيحة" });

            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الموعد غير موجود" });

            if (!IsAdmin && !await IsPartyAsync(appointment))
                return Forbid();

            // المريض (مش أدمن) يلغي بس
            if (!IsAdmin && User.IsInRole("Patient") && statusDto.Status != "Cancelled")
                return Forbid();

            appointment.Status = statusDto.Status;
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الموعد غير موجود" });

            if (!IsAdmin && !await IsPartyAsync(appointment))
                return Forbid();

            var doctor = await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId);
            var patient = await _unitOfWork.Patients.GetByIdAsync(appointment.PatientId);
            var appointmentDto = new AppointmentDto
            {
                Id = appointment.Id,
                DoctorId = appointment.DoctorId,
                DoctorFullName = $"{doctor.FirstName} {doctor.LastName}",
                PatientId = appointment.PatientId,
                PatientFullName = $"{patient.FirstName} {patient.LastName}",
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                Notes = appointment.Notes
            };
            return Ok(appointmentDto);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
            {
                return NotFound(new { message = "الموعد غير موجود" });
            }
            _unitOfWork.Appointments.Delete(appointment);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto updateAppointmentDto)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
            if (appointment == null)
                return NotFound(new { message = "الموعد غير موجود" });

            if (!IsAdmin && !await IsPartyAsync(appointment))
                return Forbid();

            if (updateAppointmentDto.AppointmentDate <= DateTime.UtcNow)
                return BadRequest(new { message = "لا يمكن التعديل لتاريخ بالماضي" });

            if (appointment.AppointmentDate != updateAppointmentDto.AppointmentDate)
            {
                // appointment.DoctorId مش updateAppointmentDto.DoctorId
                var isAvailable = await _unitOfWork.Appointments.IsDoctorAvailableAsync(appointment.DoctorId, updateAppointmentDto.AppointmentDate);
                if (!isAvailable)
                    return BadRequest(new { message = "الدكتور غير متوفر في هذا الوقت" });
            }

            appointment.AppointmentDate = updateAppointmentDto.AppointmentDate;
            appointment.Notes = updateAppointmentDto.Notes;
            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }
    }
}
