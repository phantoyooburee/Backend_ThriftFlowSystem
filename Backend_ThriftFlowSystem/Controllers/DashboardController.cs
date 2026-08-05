using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_ThriftFlowSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardServices _dashboardServices;
        private readonly IResultReplyServices _resultReply;

        public DashboardController(
            IDashboardServices dashboardServices,
            IResultReplyServices resultReply)
        {
            _dashboardServices = dashboardServices;
            _resultReply = resultReply;
        }

        // ฟังก์ชันช่วยดึง Role จาก Token (เอาไว้ใช้เฉพาะตอนที่ Service ต้องการค่าไปคำนวณ)
        private string GetCurrentEmployeeRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
        }

        // 1. สรุปยอดรวมด้านบน (Key Metrics)
        [HttpGet("Summary")]
        [Authorize] 
        public async Task<IActionResult> GetDashboardSummary([FromQuery] int? branchId)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff";
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("EmployeeId")?.Value;
                int? currentEmployeeId = int.TryParse(employeeIdClaim, out int id) ? id : null;

                var result = await _dashboardServices.GetDashboardSummaryAsync(branchId, userRole, currentEmployeeId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 2. กราฟยอดขาย (Sales Analytics)
        [HttpGet("SalesAnalytics")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetSalesAnalytics([FromQuery] string interval = "daily", [FromQuery] int days = 7, [FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _dashboardServices.GetSalesAnalyticsAsync(interval, days, branchId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 3. สินค้าขายดี (Top Performers)
        [HttpGet("TopPerformers")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetTopPerformers([FromQuery] int top = 5, [FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _dashboardServices.GetTopPerformersAsync(top, branchId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 4. แจ้งเตือนสต๊อก (Inventory Alerts: Low Stock & Deadstock)
        [HttpGet("InventoryAlerts")]
        [Authorize]
        public async Task<IActionResult> GetInventoryAlerts([FromQuery] int lowStockThreshold = 10, [FromQuery] int deadstockDays = 30, [FromQuery] int? top = null)
        {
            try
            {
                var result = await _dashboardServices.GetInventoryAlertsAsync(lowStockThreshold, deadstockDays, top);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 5. รายการบิลล่าสุด (Recent Transactions)
        [HttpGet("RecentTransactions")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int limit = 10, [FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _dashboardServices.GetRecentTransactionsAsync(limit, branchId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 6. ติดตามความคืบหน้าของแต่ละลอต (Lot Yield Tracking)
        [HttpGet("LotPerformance")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetLotPerformance([FromQuery] int limit = 5)
        {
            try
            {
                // ดึง Role เพื่อส่งไปเช็คใน Service (ซ่อน TotalCost และ Profit ถ้าเป็นแค่ Manager)
                // 👉 ตรงนี้ยังคงต้องใช้ GetCurrentEmployeeRole() เพราะ Service ต้องการรู้ว่าเป็นใคร
                var role = GetCurrentEmployeeRole();

                var result = await _dashboardServices.GetLotPerformanceAsync(role, limit);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("TopCategories")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetTopCategories([FromQuery] int top = 5, [FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _dashboardServices.GetTopCategoriesAsync(top, branchId);
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("topstaff")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetTopStaff(string interval = "monthly", int top = 5, int? branchId = null)
        {
            try
            {
                var result = await _dashboardServices.GetTopStaffAsync(interval, top, branchId );
                int statusCode = _resultReply.MapReply(result);
                return StatusCode(statusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("BranchPerformance")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetBranchPerformance()
        {
            try
            {
                var result = await _dashboardServices.GetBranchPerformanceAsync();
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("ActiveShifts")]
        [Authorize(Roles = "Owner, Manager")]
        public async Task<IActionResult> GetActiveShifts([FromQuery] int? branchId = null)
        {
            try
            {
                var result = await _dashboardServices.GetActiveShiftsPerformanceAsync(branchId);
                return StatusCode(_resultReply.MapReply(result), result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}