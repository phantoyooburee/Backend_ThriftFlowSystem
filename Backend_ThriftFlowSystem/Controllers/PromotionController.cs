using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionServices _promotionServices;
        private readonly IResultReplyServices _resultReply;

        public PromotionController(IPromotionServices promotionServices, IResultReplyServices resultReply)
        {
            _promotionServices = promotionServices;
            _resultReply = resultReply;
        }

        [HttpGet("Promotions")]
        [Authorize]
        public async Task<IActionResult> GetAllPromotions([FromQuery] bool onlyActive = false)
        {
            try
            {
                var result = await _promotionServices.GetAllPromotionsAsync(onlyActive);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
           
        }

        [HttpPost("Promotions")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }
                var result = await _promotionServices.CreatePromotionAsync(request, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("Promotions/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] PromotionRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }
                var result = await _promotionServices.UpdatePromotionAsync(id, request, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("Promotions/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> TogglePromotionActive(int id)
        {
            try
            {
                var employeeIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID not found." });
                }
                var result = await _promotionServices.TogglePromotionActiveAsync(id, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}