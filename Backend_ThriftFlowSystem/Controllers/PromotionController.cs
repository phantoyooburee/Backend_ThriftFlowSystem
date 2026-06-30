using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/controller")]
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
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionRequestDto request, int employeeId)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
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
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] PromotionRequestDto request, int employeeId)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _promotionServices.UpdatePromotionAsync(id, request, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("Promotions/{id}")]
        [Authorize(Roles = "Owner,Manager")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            try
            {
                var result = await _promotionServices.DeletePromotionAsync(id);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}