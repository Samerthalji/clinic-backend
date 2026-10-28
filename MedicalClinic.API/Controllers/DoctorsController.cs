using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Dtos.DoctorResponseDto;
using MedicalClinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Domain.Entities;
using MedicalClinic.Application.Services;

namespace MedicalClinic.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorServices;
        public DoctorsController(IDoctorService DoctorService)
        {
            _doctorServices = DoctorService; 
        }
       
        [HttpGet]
       public async Task<IActionResult> GetAllDoctors()
        {
            var doctors = await _doctorServices.GetallDoctorAsync();
            
            return Ok(doctors);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDoctorById(int id)
        {
            var doctor = await _doctorServices.GetDoctorByIdAsync(id);
                       
            return Ok(doctor);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDto updateDoctorDto)
        {
            await _doctorServices.UpdateDoctorAsync(id, updateDoctorDto);
            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            await _doctorServices.DeleteDoctorAsync(id);
            return NoContent();
        }
        [HttpGet("specialization/{specialization}")]
        public async Task<IActionResult> GetDoctorBySpecialization(string specialization)
        {
            var doctors = await _doctorServices. GetDoctorBySpecializationAsync(specialization);
          
            return Ok(doctors);
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleDoctorActiveStatus(int id)
        {
            await _doctorServices.ToggleDoctorActiveAsync(id);
            return NoContent();
        }
       
        
    }
}
