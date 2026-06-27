using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Backend_ThriftFlowSystem.DTOs.AuthenticateModels;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/auth")] 
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly IAuthenticateServices _authenServices;
        private readonly IResultReplyServices _resultReply;

        public AuthenticateController(
            IAuthenticateServices authenServices,
            IResultReplyServices resultReply)
        {
            _authenServices = authenServices;
            _resultReply = resultReply;
        }


        [HttpGet("system-status")]
        [AllowAnonymous] 
        public async Task<IActionResult> CheckSystemStatus()
        {
            try
            {
                var result = await _authenServices.CheckSystemStatusAsync();

                return Ok(result);
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            
        }

        [HttpGet("invitation/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInvitationDetails(string token)
        {
            try
            {
                var result = await _authenServices.GetInvitationDetailsAsync(token);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }

        }

        [HttpGet("Profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("Id")?.Value;

                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token payload." });
                }

                var result = await _authenServices.GetProfileAsync(employeeId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("invite")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var inviterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(inviterIdClaim) || !int.TryParse(inviterIdClaim, out int inviterId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }
                if (roleClaim != "Owner" && roleClaim != "Manager")
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "You do not have permission to invite employees. Access Denied."
                    });
                }
                // ดึง Id ของคนที่กดเชิญ ออกมาจาก JWT Token
                //var inviterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //int.TryParse(inviterIdClaim, out int inviterId);

                var result = await _authenServices.InviteEmployeeAsync(request, inviterId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return ValidationProblem(ModelState);

                var result = await _authenServices.RegisterAsync(request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var result = await _authenServices.LoginAsync(request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var result = await _authenServices.ForgotPasswordAsync(request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var result = await _authenServices.ResetPasswordAsync(request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize] 
        public async Task<IActionResult> Logout()
        {
            try
            {
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

                int empId = int.TryParse(employeeIdClaim, out int id) ? id : 0;
                string empEmail = emailClaim ?? string.Empty;

                var result = await _authenServices.LogoutAsync(empId, empEmail);

                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
