using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend_ThriftFlowSystem.Services
{
    public class GetSalesHistoryServices : IGetSalesHistoryServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<GetSalesHistoryServices> _logger;
        private readonly IWebHostEnvironment _env;

        public GetSalesHistoryServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            IWebHostEnvironment env,
            ILogger<GetSalesHistoryServices> logger)
        {
            _context = context;
            _reply = reply;
            _env = env;
            _logger = logger;
        }

        public async Task<ResultListReply> GetSalesHistoryAsync(SalesHistoryQueryDto request)
        {
            var reply = new ResultListReply();
            try
            {
                // ตั้งต้น Query ดึงพนักงาน และ คืนเงิน มาเพื่อเช็คสถานะ
                var query = _context.Orders
                    .Include(o => o.Employee)
                    .Include(o => o.Refunds)
                    .Include(o => o.Branch)
                    .AsQueryable();

                // กรองข้อมูล (Filters) ตามที่หน้าบ้านส่งมา
                if (!string.IsNullOrWhiteSpace(request.SearchReceipt))
                {
                    query = query.Where(o => o.ReceiptNumber.Contains(request.SearchReceipt.Trim()));
                }

                if (request.StartDate.HasValue)
                {
                    var startUtc = DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc);
                    query = query.Where(o => o.CreatedAt >= startUtc);
                }

                if (request.EndDate.HasValue)
                {
                    var endUtc = DateTime.SpecifyKind(request.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                    query = query.Where(o => o.CreatedAt < endUtc);
                }

                if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
                {
                    query = query.Where(o => o.EmployeeId == request.EmployeeId.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    query = query.Where(o => o.Status.ToUpper() == request.Status.ToUpper());
                }

                if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
                {
                    query = query.Where(o => o.PaymentMethod.ToUpper() == request.PaymentMethod.ToUpper());
                }

                if (request.BranchId.HasValue && request.BranchId.Value > 0)
                {
                    query = query.Where(o => o.BranchId == request.BranchId.Value);
                }

                if (request.POSShiftId.HasValue && request.POSShiftId.Value > 0)
                {
                    query = query.Where(o => o.POSShiftId == request.POSShiftId.Value);
                }

                // นับจำนวนทั้งหมดสำหรับ Pagination
                int totalItems = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                // ดึงข้อมูลแบบแบ่งหน้า (เรียงจากบิลล่าสุดไปเก่าสุด)
                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(o => new OrderSummaryDto
                    {
                        OrderId = o.Id,
                        ReceiptNumber = o.ReceiptNumber,
                        CreatedAt = o.CreatedAt,
                        EmployeeName = o.Employee != null ? $"{o.Employee.FirstName} {o.Employee.LastName}" : "Unknown",
                        TotalAmount = o.TotalAmount,
                        DiscountAmount = o.DiscountAmount,
                        NetAmount = o.NetAmount,
                        PaymentMethod = o.PaymentMethod,
                        PaymentSlipUrl = o.PaymentSlipUrl,
                        BranchName = o.Branch != null ? o.Branch.BranchName : "Unknown",
                        POSShiftId = o.POSShiftId,
                        Status = o.Status,
                        HasRefunds = o.Refunds.Any() // คืนค่า True ถ้ามีประวัติการคืนเงิน
                    })
                    .ToListAsync();

                // แพ็คข้อมูลส่งกลับ
                var responseData = new PaginatedOrderResponseDto
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = request.Page,
                    Items = orders
                };

                reply.Data = responseData;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during GetSalesHistoryAsync.");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected internal server error occurred";
            }

            return reply;
        }

        public async Task<ResultListReply> GetOrderDetailByIdAsync(int orderId, int currentEmployeeId, string? role)
        {
            var reply = new ResultListReply();
            try
            {
                
                var order = await _context.Orders
                    .Include(o => o.Employee) 
                    .Include(o => o.ApprovedBy)
                    .Include(o => o.Branch)
                    .Include(o => o.Promotion) 
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product) 
                    .Include(o => o.Refunds) 
                        .ThenInclude(r => r.ApprovedBy)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    reply.Result.ToErrorStatus();
                    reply.Data = "Order not found.";
                    return reply;
                }

                // ระบบป้องกัน: ถ้าไม่ใช่ Owner หรือ Manager ต้องเช็คว่าเป็นบิลของตัวเอง?
                if (role != "Owner" && role != "Manager")
                {
                    if (order.EmployeeId != currentEmployeeId)
                    {
                        _logger.LogWarning($"Security Alert: Employee {currentEmployeeId} attempted to access Order {orderId} belonging to another employee.");

                        reply.Result.ToErrorStatus();
                        reply.Data = "Access Denied: You are not authorized to view this order.";
                        return reply;
                    }
                }

                //  ประกอบร่างข้อมูลส่งกลับ (ออกแบบมาเพื่อหน้า History และ Print Receipt)
                var response = new
                {
                    OrderId = order.Id,
                    ReceiptNumber = order.ReceiptNumber,
                    Status = order.Status, 
                    PaymentMethod = order.PaymentMethod,
                    PaymentSlipUrl = order.PaymentSlipUrl,
                    CreatedAt = order.CreatedAt,
                    BranchName = order.Branch != null ? order.Branch.BranchName : "Unknown",
                    POSShiftId = order.POSShiftId,
                    // สรุปยอดเงิน
                    TotalAmount = order.TotalAmount,
                    DiscountAmount = order.DiscountAmount,
                    NetAmount = order.NetAmount,

                    // ข้อมูลพนักงานและผู้จัดการ
                    EmployeeName = order.Employee != null ? $"{order.Employee.FirstName} {order.Employee.LastName}" : "Unknown",
                    ApprovedByName = order.ApprovedBy != null ? $"{order.ApprovedBy.FirstName} {order.ApprovedBy.LastName}" : null,

                    // ข้อมูลโปรโมชั่น (เพื่อโชว์ในใบเสร็จ)
                    PromotionName = order.Promotion?.Name,
                    AppliedPromotionIds = order.AppliedPromotionIds,
                    IsSpecialPrice = order.IsSpecialPrice,

                    // รายการสินค้าในบิล
                    Items = order.OrderItems.Select(oi => new
                    {
                        ProductId = oi.ProductId,
                        Name = oi.Product?.Name ?? "Unknown",
                        SKU = oi.Product?.SKU ?? "N/A",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        SubTotal = oi.SubTotal
                    }).ToList(),

                    // สรุปรายการคืนเงิน (ถ้ามี) เพื่อให้ผู้จัดการดูประวัติย้อนหลัง
                    RefundHistory = order.Refunds.Select(r => new
                    {
                        RefundId = r.Id,
                        ProductId = r.ProductId,
                        ApprovedByName = r.ApprovedBy != null ? $"{r.ApprovedBy.FirstName} {r.ApprovedBy.LastName}" : "Unknown",
                        RefundedQuantity = r.Quantity,
                        RefundAmount = r.RefundAmount,
                        Reason = r.Reason,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                };

                reply.Data = response;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error GetOrderDetailByIdAsync for OrderId: {orderId}");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }

            return reply;
        }
    }
}
