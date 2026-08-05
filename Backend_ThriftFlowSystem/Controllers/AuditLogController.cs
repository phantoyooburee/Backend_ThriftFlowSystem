using Backend_ThriftFlowSystem.DTOs.Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogServices _auditLogServices;
        private readonly IResultReplyServices _resultReply;

        public AuditLogController(
            IAuditLogServices auditLogServices,
            IResultReplyServices resultReply)
        {
            _auditLogServices = auditLogServices;
            _resultReply = resultReply;
        }

        [HttpGet("authLogs")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetAuthLogs([FromQuery] LogQueryRequest query)
        {
            try
            {
                var result = await _auditLogServices.GetAuthLogsAsync(query);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("systemActionLogs")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetSystemActionLogs([FromQuery] LogQueryRequest query)
        {
            try
            {
                var result = await _auditLogServices.GetSystemActionLogsAsync(query);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("inventoryLogs")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetInventoryLogs([FromQuery] LogQueryRequest query)
        {
            try
            {
                var result = await _auditLogServices.GetInventoryLogsAsync(query);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("refundLogs")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetRefundLogs([FromQuery] LogQueryRequest query)
        {
            try
            {
                var result = await _auditLogServices.GetRefundLogsAsync(query);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("shiftLogs")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetShiftLogs([FromQuery] LogQueryRequest query)
        {
            try
            {
                var result = await _auditLogServices.GetPOSShiftLogsAsync(query);
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
