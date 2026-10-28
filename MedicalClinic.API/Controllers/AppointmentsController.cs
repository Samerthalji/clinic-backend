using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Security.Claims;
 using MedicalClinic.Application.Exceptions;

namespace MedicalClinic.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appServices;
        public AppointmentsController(IAppointmentService AppServices)
        {
            _appServices = AppServices;
        }
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin => User.IsInRole("Admin");
        private bool IsDoctor => User.IsInRole("Doctor");
       
        // هل المستخدم الحالي طرف بالموعد (مريضه أو دكتوره)؟
       
        [Authorize(Roles = "Patient")]
        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            var result = await _appServices.CreateAsync(CurrentUserId, dto);
            return CreatedAtAction(nameof(GetAppointmentById), new { id = result.Id }, result);
        }
        // المريض يشوف مواعيده
        [Authorize(Roles = "Patient")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var result = await _appServices.GetMyAppointmentsAsync(CurrentUserId);
            return Ok(result);
        }

        // الدكتور يشوف مواعيده
        [Authorize(Roles = "Doctor")]
        [HttpGet("doctor/my")]
        public async Task<IActionResult> GetMyDoctorAppointments()
        {
            var result = await _appServices.GetMyDoctorAppointmentsAsync(CurrentUserId);
            return Ok(result);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("doctor/{doctorId:int}")]
        public async Task<IActionResult> GetDoctorAppointments(int doctorId)
        {
            var results = await _appServices.GetDoctorAppointmentsAsync(doctorId);
            return Ok(results);

        }
        [Authorize(Roles = "Admin")]
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetPatientAppointments(int patientId)
        {

            var results = await _appServices.GetDoctorAppointmentsAsync(patientId);
            return Ok(results);
        }
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusDto statusDto)
        {
            await _appServices.UpdateStatusAsync(id, statusDto, CurrentUserId, IsAdmin, IsDoctor);
            return NoContent();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            var result = await _appServices.GetByIdAsync(id, CurrentUserId, IsAdmin);
            return Ok(result);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            await _appServices.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto updateAppointmentDto)
        {
            await _appServices.UpdateAsync(id, updateAppointmentDto, CurrentUserId, IsAdmin);
            return NoContent();
        }
    }
}