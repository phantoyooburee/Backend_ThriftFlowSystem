using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.DTOs.Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend_ThriftFlowSystem.Services
{
    public class AuditLogServices : IAuditLogServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AuditLogServices> _logger;

        public AuditLogServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            IWebHostEnvironment env, 
            ILogger<AuditLogServices> logger)
        {
            _context = context;
            _reply = reply;
            _env = env;
            _logger = logger;
        }

        public async Task<ResultListReply> GetAuthLogsAsync(LogQueryRequest query)
        {
            var reply = new ResultListReply();
            try
            {
                var q = _context.AuthLogs
                    .Include(l => l.Employee)
                    .Include(l => l.ActorEmployee)
                    .AsQueryable();

                if (query.From.HasValue)
                {
                    var fromUtc = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
                    q = q.Where(l => l.Timestamp >= fromUtc);
                }
                if (query.To.HasValue)
                {
                    var toUtc = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
                    q = q.Where(l => l.Timestamp < toUtc);
                }
                if (query.EmployeeId.HasValue) q = q.Where(l => l.EmployeeId == query.EmployeeId.Value);
                if (!string.IsNullOrWhiteSpace(query.Action))
                {
                    var actionKeyword = query.Action.Trim().ToLower();
                    // ใช้ .Contains() แทน == และเช็ค null กันพัง
                    q = q.Where(l => l.Action != null && l.Action.ToLower() == (actionKeyword));
                }

                int totalItems = await q.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);
                if (query.Page > totalPages && totalPages > 0) query.Page = 1;
                if (query.Page < 1) query.Page = 1;

                var items = await q
                    .OrderByDescending(l => l.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(l => new AuthLogResponse
                    {
                        Id = l.Id,
                        Timestamp = l.Timestamp,
                        Action = l.Action,
                        EmployeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : null,
                        ActorName = l.ActorEmployee != null ? $"{l.ActorEmployee.FirstName} {l.ActorEmployee.LastName}" : null,
                        TargetEmail = l.TargetEmail,
                        IPAddress = l.IPAddress,
                        UserAgent = l.UserAgent,
                        Details = l.Details
                    }).ToListAsync();

                reply.Data = new PagedLogResponse<AuthLogResponse>
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = query.Page, 
                    Items = items
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetAuthLogsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> GetSystemActionLogsAsync(LogQueryRequest query, string? targetTable = null)
        {
            var reply = new ResultListReply();
            try
            {
                var q = _context.SystemActionLogs
                    .Include(l => l.Employee)
                    .AsQueryable();

                if (query.From.HasValue)
                {
                    var fromUtc = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
                    q = q.Where(l => l.CreatedAt >= fromUtc);
                }
                if (query.To.HasValue)
                {
                    var toUtc = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
                    q = q.Where(l => l.CreatedAt < toUtc);
                }
                if (query.EmployeeId.HasValue) q = q.Where(l => l.EmployeeId == query.EmployeeId.Value);

                var tableToSearch = query.TargetTable ?? targetTable;
                if (!string.IsNullOrWhiteSpace(tableToSearch))
                    q = q.Where(l => l.TargetTable.ToUpper() == tableToSearch.ToUpper());

                if (!string.IsNullOrWhiteSpace(query.Action))
                {
                    var actionKeyword = query.Action.Trim().ToLower();
                    q = q.Where(l => l.ActionType != null && l.ActionType.ToLower() == (actionKeyword));
                }

                int totalItems = await q.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);
                if (query.Page > totalPages && totalPages > 0) query.Page = 1;
                if (query.Page < 1) query.Page = 1;

                var items = await q
                    .OrderByDescending(l => l.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(l => new SystemActionLogResponse
                    {
                        Id = l.Id,
                        CreatedAt = l.CreatedAt,
                        EmployeeId = l.EmployeeId,
                        EmployeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : "Unknown",
                        ActionType = l.ActionType,
                        TargetTable = l.TargetTable,
                        TargetRecordId = l.TargetRecordId,
                        Details = l.Details
                    }).ToListAsync();

                reply.Data = new PagedLogResponse<SystemActionLogResponse>
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = query.Page,
                    Items = items
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetSystemActionLogsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }
        public async Task<ResultListReply> GetInventoryLogsAsync(LogQueryRequest query, string? actionType = null, int? productId = null)
        {
            var reply = new ResultListReply();
            try
            {
                var q = _context.InventoryLogs
                    .Include(l => l.Employee)
                    .Include(l => l.Product)
                    .AsQueryable();

                if (query.From.HasValue)
                {
                    var fromUtc = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
                    q = q.Where(l => l.CreatedAt >= fromUtc);
                }
                if (query.To.HasValue)
                {
                    var toUtc = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
                    q = q.Where(l => l.CreatedAt < toUtc);
                }
                if (query.EmployeeId.HasValue) q = q.Where(l => l.EmployeeId == query.EmployeeId.Value);
                if (productId.HasValue) q = q.Where(l => l.ProductId == productId.Value);

                var actionToSearch = query.Action ?? actionType;
                if (!string.IsNullOrWhiteSpace(query.Action))
                {
                    var actionKeyword = query.Action.Trim().ToLower();
                    q = q.Where(l => l.ActionType != null && l.ActionType.ToLower() == (actionKeyword));
                }

                if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
                {
                    var keyword = query.SearchKeyword.Trim().ToLower();
                    q = q.Where(l => l.Product != null &&
                                     (l.Product.Name.ToLower().Contains(keyword) ||
                                      l.Product.SKU.ToLower().Contains(keyword)));
                }

                int totalItems = await q.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);
                if (query.Page > totalPages && totalPages > 0) query.Page = 1;
                if (query.Page < 1) query.Page = 1;

                var items = await q
                    .OrderByDescending(l => l.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(l => new InventoryLogResponse
                    {
                        Id = l.Id,
                        CreatedAt = l.CreatedAt,
                        EmployeeId = l.EmployeeId,
                        EmployeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : "Unknown",
                        ProductId = l.ProductId,
                        ProductName = l.Product != null ? l.Product.Name : "Unknown",
                        ProductSKU = l.Product != null ? l.Product.SKU : "N/A",
                        ActionType = l.ActionType,
                        QuantityChanged = l.QuantityChanged,
                        Note = l.Note
                    }).ToListAsync();

                reply.Data = new PagedLogResponse<InventoryLogResponse>
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = query.Page,
                    Items = items
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetInventoryLogsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> GetRefundLogsAsync(LogQueryRequest query)
        {
            var reply = new ResultListReply();
            try
            {
                var q = _context.Refunds
                    .Include(r => r.Employee)
                    .Include(r => r.ApprovedBy)
                    .Include(r => r.Order)
                    .Include(r => r.Product)
                    .AsQueryable();

                if (query.From.HasValue)
                {
                    var fromUtc = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
                    q = q.Where(r => r.CreatedAt >= fromUtc);
                }
                if (query.To.HasValue)
                {
                    var toUtc = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
                    q = q.Where(r => r.CreatedAt < toUtc);
                }
                if (query.EmployeeId.HasValue) q = q.Where(r => r.EmployeeId == query.EmployeeId.Value);

                int totalItems = await q.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);
                if (query.Page > totalPages && totalPages > 0) query.Page = 1;
                if (query.Page < 1) query.Page = 1;

                var items = await q
                    .OrderByDescending(r => r.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(r => new RefundLogResponse
                    {
                        Id = r.Id,
                        CreatedAt = r.CreatedAt,
                        OrderId = r.OrderId,
                        ReceiptNumber = r.Order != null ? r.Order.ReceiptNumber : "Unknown",
                        ProductId = r.ProductId,
                        ProductName = r.Product != null ? r.Product.Name : "Unknown",
                        EmployeeId = r.EmployeeId,
                        EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : "Unknown",
                        ApprovedById = r.ApprovedById,
                        ApprovedByName = r.ApprovedBy != null ? $"{r.ApprovedBy.FirstName} {r.ApprovedBy.LastName}" : "Unknown",
                        Quantity = r.Quantity,
                        RefundAmount = r.RefundAmount,
                        Reason = r.Reason
                    }).ToListAsync();

                reply.Data = new PagedLogResponse<RefundLogResponse>
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = query.Page, 
                    Items = items
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetRefundLogsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> GetPOSShiftLogsAsync(LogQueryRequest query, int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                
                var q = _context.POSShifts
                    .Include(s => s.Employee)
                    .Include(s => s.Branch)
                    .AsQueryable();


                if (query.From.HasValue)
                {
                    var fromUtc = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
                    q = q.Where(s => s.StartTime >= fromUtc);
                }
                if (query.To.HasValue)
                {
                    var toUtc = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
                    q = q.Where(s => s.StartTime < toUtc);
                }
                if (query.EmployeeId.HasValue) q = q.Where(s => s.EmployeeId == query.EmployeeId.Value);
                if (branchId.HasValue) q = q.Where(s => s.BranchId == branchId.Value);

                if (!string.IsNullOrWhiteSpace(query.Status))
                    q = q.Where(s => s.Status.ToUpper() == query.Status.ToUpper());

                int totalItems = await q.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);
                if (query.Page > totalPages && totalPages > 0) query.Page = 1;
                if (query.Page < 1) query.Page = 1;

                var items = await q
                    .OrderByDescending(s => s.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(s => new POSShiftLogResponse
                    {
                        Id = s.Id,
                        BranchId = s.BranchId,
                        BranchName = s.Branch != null ? s.Branch.BranchName : "Unknown",
                        EmployeeId = s.EmployeeId,
                        EmployeeName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : "Unknown",
                        StartingCash = s.StartingCash,
                        CashInAmount = s.CashInAmount,
                        CashOutAmount = s.CashOutAmount,
                        ExpectedCash = s.ExpectedCash,
                        ActualCash = s.ActualCash,
                        Difference = s.Difference,
                        Status = s.Status,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Remarks = s.Remarks
                    }).ToListAsync();

                reply.Data = new PagedLogResponse<POSShiftLogResponse>
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = query.Page,
                    Items = items
                };
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetPOSShiftLogsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            }
            return reply;
        }
    }
}
