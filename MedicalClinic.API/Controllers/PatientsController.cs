using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Dtos.PatientResponseDto;
using MedicalClinic.Application.Interfaces;
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
        private readonly IUnitOfWork _unitOfWork;

        public PatientsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet]
        public async Task<IActionResult> GetAllPatients([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
         
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            
            var patients = await _unitOfWork.Patients.GetPagedPatientsAsync(pageNumber, pageSize);
            var totalCount = await _unitOfWork.Patients.GetTotalCountAsync();
            var patientDtos = patients.Select(p => new PatientDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Address = p.Address
            }).ToList();
            var response = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = patientDtos
            };
            return Ok(response);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPatientById(int id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
            {
                return NotFound(new { Message = "Patient not found" });
            }
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Doctor");
            if (!isStaff && patient.IdentityUserId != CurrentUserId)
                return Forbid();
            var patientDto = new PatientDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                PhoneNumber = patient.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Address = patient.Address
            };
            return Ok(patientDto);
        }
     
      
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] UpdatePatientDto updatePatientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
            {
                return NotFound(new { Message = "Patient not found" });
            }
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && patient.IdentityUserId != CurrentUserId)
                return Forbid();
            patient.FirstName = updatePatientDto.FirstName;
            patient.LastName = updatePatientDto.LastName;
            patient.PhoneNumber = updatePatientDto.PhoneNumber;
            patient.DateOfBirth = updatePatientDto.DateOfBirth;
            patient.Address = updatePatientDto.Address;
            _unitOfWork.Patients.Update(patient);
            await _unitOfWork.CompleteAsync();
            return NoContent(); ;
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(id);
            if (patient == null)
            {
                return NotFound(new { Message = "Patient not found" });
            }
            _unitOfWork.Patients.Delete(patient);
            await _unitOfWork.CompleteAsync();
            return Ok(new { Message = "Patient deleted successfully" });
        }
        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet("phone/{phoneNumber}")]
        public async Task<IActionResult> GetPatientByPhoneNumber(string phoneNumber)
        {
            
    
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return BadRequest(new { Message = "Phone number is required" });
            var patient = await _unitOfWork.Patients.GetPatientByPhoneNumberAsync(phoneNumber);
            if (patient == null)
            {
                return NotFound(new { Message = "Patient not found" });
            }   
            var patientDto = new PatientDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                PhoneNumber = patient.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Address = patient.Address
            };
            return Ok(patientDto);
        }

        [Authorize(Roles = "Admin,Doctor")]
        [HttpGet("search")]
        public async Task<IActionResult> GetPatientsByName([FromQuery] string name)
        {
            var patients = await _unitOfWork.Patients.GetPatientsByNameAsync(name);
            if (patients == null || !patients.Any())
            {
                return NotFound(new { Message = "No patients found with the given name" });
            }
            var patientDtos = patients.Select(p => new PatientDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Address = p.Address
            });
            return Ok(patientDtos);
        }
        [Authorize(Roles = "Patient")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var patient = await _unitOfWork.Patients.GetByIdentityUserIdAsync(CurrentUserId);
            if (patient == null)
                return NotFound(new { Message = "Patient not found" });

            return Ok(new PatientDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                PhoneNumber = patient.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Address = patient.Address
            });
        }

    }
}
