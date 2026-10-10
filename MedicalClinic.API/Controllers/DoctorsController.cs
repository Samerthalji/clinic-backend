using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Dtos.DoctorResponseDto;
using MedicalClinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalClinic.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public DoctorsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [HttpGet]
       public async Task<IActionResult> GetAllDoctors()
        {
            var doctors = await _unitOfWork.Doctors.GetAllAsync();
            var doctorDtos = doctors.Select(d => new DoctorDto
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
            }).ToList();
            return Ok(doctorDtos);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDoctorById(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            var doctorDto = new DoctorDto
            {
                Id = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Specialization = doctor.Specialization,
                ClinicRoom = doctor.ClinicRoom,
                Email = doctor.Email,
                PhoneNumber = doctor.PhoneNumber,
                ConsultationFee = doctor.ConsultationFee,
                IsActive = doctor.IsActive
            };

            return Ok(doctorDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDto updateDoctorDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            doctor.FirstName = updateDoctorDto.FirstName;
            doctor.LastName = updateDoctorDto.LastName;
            doctor.Specialization = updateDoctorDto.Specialization;
            doctor.ClinicRoom = updateDoctorDto.ClinicRoom;
            doctor.Email = updateDoctorDto.Email;
            doctor.PhoneNumber = updateDoctorDto.PhoneNumber;
            doctor.ConsultationFee = updateDoctorDto.ConsultationFee;
            doctor.IsActive = updateDoctorDto.IsActive;
            _unitOfWork.Doctors.Update(doctor);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            _unitOfWork.Doctors.Delete(doctor);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }
        [HttpGet("specialization/{specialization}")]
        public async Task<IActionResult> GetDoctorBySpecialization(string specialization)
        {
            var doctors = await _unitOfWork.Doctors.GetDoctorBySpecializationAsync(specialization);
          
            var doctorDtos = doctors.Select(d => new DoctorDto
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
            });
            return Ok(doctorDtos);
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleDoctorActiveStatus(int id)
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            doctor.IsActive = !doctor.IsActive;
            _unitOfWork.Doctors.Update(doctor);
            await _unitOfWork.CompleteAsync();
            return NoContent();
        }
       
        
    }
}
