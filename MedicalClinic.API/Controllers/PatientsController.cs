using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Dtos.PatientResponseDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalClinic.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientServices _patientServices;

        public PatientsController(IPatientServices patientServices)
        {
            _patientServices = patientServices;
        }
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin => User.IsInRole("Admin");
        private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("Doctor");



        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet]
        public async Task<IActionResult> GetAllPatients([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {


            var result = await _patientServices.GetAllAsync(pageNumber, pageSize);
            return Ok (result);

            
           
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            var result = _patientServices.GetByIdAsync(id,CurrentUserId,IsStaff);

            return Ok(result);
        }
     
      
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] UpdatePatientDto updatePatientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _patientServices.UpdateAsync(id, updatePatientDto, CurrentUserId, IsAdmin);
           
            return NoContent(); ;
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            await _patientServices.DeleteAsync(id);
           
            return Ok(new { Message = "Patient deleted successfully" });
        }
       
        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet("search")]
        public async Task<IActionResult> GetPatientsByName([FromQuery] string name)
        {

            var result = await _patientServices.SearchByNameAsync(name);
            return Ok(result);
        }
        [Authorize(Roles = "Patient")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var result = await _patientServices.GetMeAsync(CurrentUserId);

            return Ok(result);
         
        }

    }
}
