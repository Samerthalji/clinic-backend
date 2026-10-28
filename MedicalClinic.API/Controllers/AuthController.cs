using MedicalClinic.Application.Dtos.AuthrResponseDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalClinic.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModelDto request)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.RegisterAsync(request);
            if(!result.IsAuthenticated)
            {
                throw new BadRequestException(result.Message);
            }  

            return Ok(result);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("RegisterWithRole")]
        public async Task<IActionResult> RegisterWithRole([FromBody] RegisterWithRoleDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterWithRoleAsync(request, request.Role);
            if (!result.IsAuthenticated)
                throw new BadRequestException(result.Message);

            // ما برجّع الـ tokens للأدمن، هاي بيانات المستخدم الجديد مش الأدمن
            return Ok(new { message = "User created", result.Username, result.Email, result.Roles });
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] TokenRequestModelDto request)
        {
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.GetTokenAsync(request);
            if (!result.IsAuthenticated)
            {
                throw new BadRequestException(result.Message);
            }
            return Ok(result);
        }
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.RefreshTokenAsync(request.refreshToken);
            if (!result.IsAuthenticated)
            {
                throw new BadRequestException(result.Message);
            }
            return Ok(result);
        }
        [HttpPost("RevokeToken")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.RevokeTokenAsync(request.refreshToken);
            if (!result)
            {
                throw new BadRequestException("Token is invalid or already revoked");
               
            }
                return Ok(new { message = "Token revoked successfully" });
            }
    }
}
