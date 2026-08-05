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

        [HttpGet("Employees")]
        [Authorize(Roles ="Owner,Manager")]
        public async Task<IActionResult> GerEmployees()
        {
            try
            {
                var result = await _authenServices.GetEmployeesAsync();

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

                if (string.IsNullOrEmpty(inviterIdClaim) || !int.TryParse(inviterIdClaim, out int inviterId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

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

        [HttpPatch("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] EmployeeUpdateRequest request)
        {
            try
            {
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }

                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var result = await _authenServices.UpdateProfileAsync(employeeId, request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }

                if (request == null)
                    return BadRequest(new { error = "Request body is required." });

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                var result = await _authenServices.ChangePasswordAsync(employeeId, request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("reset-pin-with-password")]
        [Authorize]
        public async Task<IActionResult> ResetPinWithPassword([FromBody] ResetPinWithPasswordRequest request)
        {
            try
            {
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);
                var result = await _authenServices.ResetPinWithPasswordAsync(employeeId, request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("change-pin")]
        [Authorize]
        public async Task<IActionResult> ChangePin([FromBody] ChagePinRequest request)
        {
            try
            {
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);
                var result = await _authenServices.ChangePinAsync(employeeId, request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("admin-force-reset-pin/{employeeId}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> AdminForceResetPin(int employeeId, [FromBody] AdminForceResetPinRequest request)
        {
            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(adminIdClaim, out int actorId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);
                var result = await _authenServices.AdminForceResetPinAsync(employeeId, request, actorId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("change-role/{employeeId}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> ChangeRole(int employeeId, [FromBody] ChangeRoleRequest request)
        {
            try
            {
                var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(actorIdClaim, out int actorId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }
                if (request == null)
                    return BadRequest(new { error = "Request body is required." });
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);
                var result = await _authenServices.ChangeRoleAsync(employeeId, request, actorId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("toggle-active/{employeeId}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> ToggleEmployeeActive(int employeeId)
        {
            try
            {
                var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(actorIdClaim, out int actorId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }
                var result = await _authenServices.ToggleEmployeeActiveAsync(employeeId, actorId);
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

                int employeeId = int.TryParse(employeeIdClaim, out int id) ? id : 0;
                string empEmail = emailClaim ?? string.Empty;

                var result = await _authenServices.LogoutAsync(employeeId, empEmail);

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
