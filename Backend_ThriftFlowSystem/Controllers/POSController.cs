using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class POSController : ControllerBase
    {
        private readonly IPOSServices _posServices;
        private readonly IResultReplyServices _resultReply;

        public POSController(IPOSServices posServices, IResultReplyServices resultReply)
        {
            _posServices = posServices;
            _resultReply = resultReply;
        }

        [HttpPost("checkout")]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> Checkout([FromForm] CheckoutRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

               
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

               
                var result = await _posServices.CheckoutAsync(request, employeeId);
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