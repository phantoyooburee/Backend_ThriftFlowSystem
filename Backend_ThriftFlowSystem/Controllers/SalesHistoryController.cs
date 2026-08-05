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
    public class SalesHistoryController : ControllerBase
    {
        private readonly IGetSalesHistoryServices _salesHistoryServices;
        private readonly IResultReplyServices _resultReply;

        public SalesHistoryController(
            IGetSalesHistoryServices salesHistoryServices,
            IResultReplyServices resultReply)
        {
            _salesHistoryServices = salesHistoryServices;
            _resultReply = resultReply;
        }

        private int GetCurrentEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(claim, out int employeeId);
            return employeeId;
        }

        private string? GetCurrentEmployeeRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        [HttpGet("SalesHistory")]
        [Authorize]
        public async Task<IActionResult> GetSalesHistory([FromQuery] SalesHistoryQueryDto request)
        {
            try
            {
                var role = GetCurrentEmployeeRole();
                int currentEmployeeId = GetCurrentEmployeeId();


                if (role != "Owner" && role != "Manager")
                {
 
                    request.EmployeeId = currentEmployeeId;
                }

                var result = await _salesHistoryServices.GetSalesHistoryAsync(request);

                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("SalesHistory/{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            try
            {
                var role = GetCurrentEmployeeRole();
                int currentEmployeeId = GetCurrentEmployeeId();

                var result = await _salesHistoryServices.GetOrderDetailByIdAsync(id, currentEmployeeId, role);

                // ดักจับพิเศษ: กรณีหาไม่เจอ หรือ Staff แอบดูบิลคนอื่น (Access Denied)
                // คืนค่า 404 NotFound ไปเลยเพื่อความเนียน ไม่ให้หน้าบ้านรู้ว่ามีบิลนี้อยู่จริง
                if (result.Result.Value == "F" &&
                   (result.Data?.ToString() == "Order not found." ||
                    result.Data?.ToString()?.Contains("Access Denied") == true))
                {
                    return StatusCode(StatusCodes.Status404NotFound, result);
                }

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