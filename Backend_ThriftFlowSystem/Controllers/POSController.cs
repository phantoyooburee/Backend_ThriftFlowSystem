using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpPost("calculate-cart")]
        public async Task<IActionResult> CalculateCart([FromBody] CalculateCartRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _posServices.CalculateCartAsync(request);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
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

        [HttpPatch("uploadSlipLater")]
        [Authorize]
        public async Task<IActionResult> UploadSlipLater([FromForm] UploadSlipRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                // ดึงค่า OrderId และ SlipImage ออกมาจาก request DTO
                var result = await _posServices.UploadSlipLaterAsync(request.OrderId, request.SlipImage, employeeId);

                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("orders/search")]
        [Authorize]
        public async Task<IActionResult> GetOrderByReceiptNumber([FromQuery] string receiptNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(receiptNumber))
                {
                    return BadRequest(new { message = "Receipt number is required." });
                }

                // เรียกใช้ Service แทนการดึง Database ตรงๆ
                var result = await _posServices.SearchOrderByReceiptAsync(receiptNumber);

                // จัดการกรณีหาไม่เจอ (404)
                if (result.Result.Value == "F" && result.Data?.ToString() == "Order not found.")
                {
                    return NotFound(new { message = "Order not found." });
                }

                // ใช้ _resultReply Map สถานะกลับไป (200 OK หรือ Error อื่นๆ)
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("refund")]
        [Authorize] 
        public async Task<IActionResult> RefundOrder([FromBody] RefundRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var employeeIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                var result = await _posServices.ProcessRefundAsync(request, employeeId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("shift/{branchId}")]
        [Authorize]
        public async Task<IActionResult> GetActiveShift(int branchId)
        {
            try
            {
                var result = await _posServices.GetActiveShiftAsync(branchId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("shift/open")]
        [Authorize]
        public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                var result = await _posServices.OpenShiftAsync(employeeId, request);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("shift/close")]
        [Authorize]
        public async Task<IActionResult> CloseShift(int shiftId, [FromBody] CloseShiftRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                var result = await _posServices.CloseShiftAsync(shiftId, employeeId, request);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("shift/{branchId}/cash-transaction")]
        [Authorize]
        public async Task<IActionResult> AddCashTransaction(int branchId, [FromBody] CashTransactionRequest request)
        {
            try
            {
                // 1. เช็กว่าข้อมูลที่ส่งมาครบถ้วนตาม DTO ไหม
                if (!ModelState.IsValid) return BadRequest(ModelState);

                // 2. ดึงรหัสพนักงาน (คนที่กดทำรายการ) จาก Token
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                // 3. ส่งข้อมูลไปให้ Service ทำงาน
                var result = await _posServices.AddCashTransactionAsync(branchId, employeeId, request);

                // 4. ส่ง Status Code และผลลัพธ์กลับไปให้ Frontend
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}