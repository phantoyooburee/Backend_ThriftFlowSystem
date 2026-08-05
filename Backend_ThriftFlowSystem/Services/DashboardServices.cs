using Backend_ThriftFlowSystem.Data;
using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend_ThriftFlowSystem.Services
{
    public class DashboardServices : IDashboardServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IResultReplyServices _reply;
        private readonly ILogger<DashboardServices> _logger;
        private readonly IWebHostEnvironment _env;

        public DashboardServices(
            ApplicationDbContext context,
            IResultReplyServices reply,
            ILogger<DashboardServices> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _reply = reply;
            _logger = logger;
            _env = env;
        }

        // สรุปยอดรวมด้านบน (Key Metrics)
        public async Task<ResultListReply> GetDashboardSummaryAsync(int? branchId = null, string userRole = "Owner", int? currentEmployeeId = null)
        {
            var reply = new ResultListReply();
            try
            {
                
                var startOfTodayThai = DateTime.UtcNow.AddHours(7).Date;
                var startOfTodayUTC = DateTime.SpecifyKind(startOfTodayThai.AddHours(-7), DateTimeKind.Utc);

                var ordersQuery = _context.Orders.AsQueryable();

               
                if (branchId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.BranchId == branchId.Value);
                }

                // กรองตามพนักงาน (ถ้าเป็น Staff ให้ดูได้แค่บิลของตัวเอง)
                if (userRole == "Staff" && currentEmployeeId.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.EmployeeId == currentEmployeeId.Value);
                }

                
                var todayOrders = await ordersQuery
                    .Where(o => o.CreatedAt >= startOfTodayUTC && o.Status == "COMPLETED")
                    .ToListAsync();

               
                var pendingSlipsCount = await ordersQuery
                    .CountAsync(o => o.PaymentMethod == "TRANSFER" &&  o.PaymentSlipUrl == null || o.PaymentSlipUrl == "" && o.Status == "COMPLETED");

               
                var lowStockCount = await _context.Products
                    .CountAsync(p => p.IsActive && p.QuantityInStock > 0 && p.QuantityInStock <= 10 && p.IsGenericSKU == true);

                var summary = new DashboardSummaryDto
                {
                    TodaySales = todayOrders.Sum(o => o.NetAmount),
                    TodayTotalOrders = todayOrders.Count,
                    PendingSlipsCount = pendingSlipsCount,
                    LowStockItemsCount = lowStockCount
                };

                reply.Data = summary;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetDashboardSummaryAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        // กราฟยอดขาย (Sales Analytics)
        public async Task<ResultListReply> GetSalesAnalyticsAsync(string interval = "daily", int days = 7, int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                var startDate = DateTime.UtcNow.AddHours(7).Date.AddDays(-days + 1);

                var query = _context.Orders
                    .Where(o => o.CreatedAt >= startDate && o.Status == "COMPLETED")
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(o => o.BranchId == branchId.Value);
                }

                List<SalesChartDto> salesData;

                if (interval.ToLower() == "monthly")
                {

                    var rawMonthlyData = await query
                        .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            CashSales = g.Where(o => o.PaymentMethod == "CASH").Sum(o => o.NetAmount),
                            TransferSales = g.Where(o => o.PaymentMethod == "TRANSFER").Sum(o => o.NetAmount),
                            TotalSales = g.Sum(o => o.NetAmount)
                        })
                        .OrderBy(d => d.Year).ThenBy(d => d.Month)
                        .ToListAsync(); 

                    // ใช้ C# แปลง String ใน Memory
                    salesData = rawMonthlyData.Select(d => new SalesChartDto
                    {
                        Date = $"{d.Year}-{d.Month:D2}",
                        CashSales = d.CashSales,
                        TransferSales = d.TransferSales,
                        TotalSales = d.TotalSales
                    }).ToList();
                }
                else
                {

                    var rawDailyData = await query
                        .GroupBy(o => o.CreatedAt.AddHours(7).Date)
                        .Select(g => new
                        {
                            Date = g.Key, 
                            CashSales = g.Where(o => o.PaymentMethod == "CASH").Sum(o => o.NetAmount),
                            TransferSales = g.Where(o => o.PaymentMethod == "TRANSFER").Sum(o => o.NetAmount),
                            TotalSales = g.Sum(o => o.NetAmount)
                        })
                        .OrderBy(d => d.Date)
                        .ToListAsync(); 


                    salesData = rawDailyData.Select(d => new SalesChartDto
                    {
                        Date = d.Date.ToString("yyyy-MM-dd"), 
                        CashSales = d.CashSales,
                        TransferSales = d.TransferSales,
                        TotalSales = d.TotalSales
                    }).ToList();
                }

                reply.Data = salesData;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetSalesAnalyticsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        // สินค้าขายดี (Top Performers)
        public async Task<ResultListReply> GetTopPerformersAsync(int top = 5, int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.Status == "COMPLETED")
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(oi => oi.Order!.BranchId == branchId.Value);
                }

                var topProducts = await query
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.SKU })
                    .Select(g => new TopPerformerDto
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        SKU = g.Key.SKU,
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.SubTotal)
                    })
                    .OrderByDescending(p => p.TotalQuantitySold) // จัดอันดับตามจำนวนชิ้น
                    .Take(top)
                    .ToListAsync();

                reply.Data = topProducts;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetTopPerformersAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        // แจ้งเตือนสต๊อก (Low Stock & Deadstock)
        public async Task<ResultListReply> GetInventoryAlertsAsync(int lowStockThreshold = 10, int deadstockDays = 30, int? top = null)
        {
            var reply = new ResultListReply();
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-deadstockDays);
                var alerts = new List<InventoryAlertDto>();

                // หา Low Stock
                var lowStockProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.QuantityInStock > 0 && p.QuantityInStock <= lowStockThreshold && p.IsGenericSKU == true)
                    .Select(p => new InventoryAlertDto
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        SKU = p.SKU ?? "N/A",
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown",
                        QuantityInStock = p.QuantityInStock,
                        SellingPrice = p.SellingPrice,
                        AlertType = "LOW_STOCK",
                        DaysInStock = (int)(DateTime.UtcNow - p.CreatedAt).TotalDays
                    })
                    .ToListAsync();

                alerts.AddRange(lowStockProducts);

                // หา Deadstock (ใช้ Logic สุดเป๊ะของคุณ Pantho)
                var soldProductIds = await _context.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt >= cutoffDate && oi.Order.Status == "COMPLETED")
                    .Select(oi => oi.ProductId)
                    .Distinct()
                    .ToListAsync();

                var lowStockIds = lowStockProducts.Select(l => l.ProductId).ToList();

                var deadstockProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p =>
                        p.IsActive &&
                        p.QuantityInStock > 0 &&
                        p.CreatedAt <= cutoffDate && 
                        !soldProductIds.Contains(p.Id) && !lowStockIds.Contains(p.Id))
                    .Select(p => new InventoryAlertDto
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        SKU = p.SKU ?? "N/A",
                        CategoryName = p.Category != null ? p.Category.Name : "Unknown",
                        QuantityInStock = p.QuantityInStock,
                        SellingPrice = p.SellingPrice,
                        AlertType = "DEADSTOCK",
                        DaysInStock = (int)(DateTime.UtcNow - p.CreatedAt).TotalDays
                    })
                    .ToListAsync();

                alerts.AddRange(deadstockProducts);

                var sortedAlerts = alerts.OrderByDescending(a => a.DaysInStock);
                if (top.HasValue && top.Value > 0)
                {
                    reply.Data = sortedAlerts.Take(top.Value).ToList();
                }
                else
                {
                    reply.Data = sortedAlerts.ToList();
                }


                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetInventoryAlertsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        //  รายการบิลล่าสุด (Recent Transactions)
        public async Task<ResultListReply> GetRecentTransactionsAsync(int limit = 10, int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.Orders
                    .Include(o => o.Employee)
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(o => o.BranchId == branchId.Value);
                }

                var recentOrders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(limit)
                    .Select(o => new RecentTransactionDto
                    {
                        OrderId = o.Id,
                        ReceiptNumber = o.ReceiptNumber,
                        CreatedAt = o.CreatedAt,
                        NetAmount = o.NetAmount,
                        PaymentMethod = o.PaymentMethod,
                        EmployeeName = o.Employee != null ? $"{o.Employee.FirstName} {o.Employee.LastName}" : "Unknown"
                    })
                    .ToListAsync();

                reply.Data = recentOrders;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetRecentTransactionsAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        //  ติดตามความคืบหน้าของแต่ละลอต (Lot Yield Tracking)
        public async Task<ResultListReply> GetLotPerformanceAsync(string userRole, int limit = 5)
        {
            var reply = new ResultListReply();
            try
            {
                var lots = await _context.ProductLots
                    .Where(l => l.IsActive)
                    .OrderByDescending(l => l.Id)
                    .Take(limit)
                    .Select(l => new LotPerformanceDto
                    {
                        LotId = l.Id,
                        LotName = l.LotName,
                        TotalReceived = l.ReceivedQuantity,
                        TotalProcessed = l.AllocatedQuantity,

                        TotalSold = _context.OrderItems
                            .Where(oi => oi.Product!.ProductLotId == l.Id && oi.Order!.Status == "COMPLETED")
                            .Sum(oi => (int?)oi.Quantity) ?? 0,

                        TotalRevenue = _context.OrderItems
                            .Where(oi => oi.Product!.ProductLotId == l.Id && oi.Order!.Status == "COMPLETED")
                            .Sum(oi => (decimal?)oi.SubTotal) ?? 0,

                        TotalCost = l.TotalLotCost
                    })
                    .ToListAsync();

                foreach (var lot in lots)
                {
                    if (userRole != "Owner")
                    {
                        lot.TotalCost = null;
                        lot.Profit = null;
                    }
                    else
                    {
                        lot.Profit = lot.TotalRevenue - lot.TotalCost;
                    }
                }

                reply.Data = lots;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetLotPerformanceAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        // หมวดหมู่ทำเงิน (Top Categories)
        public async Task<ResultListReply> GetTopCategoriesAsync(int top = 5, int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                var query = _context.OrderItems
                    .Include(oi => oi.Product)
                    .ThenInclude(p => p!.Category) // ดึงไปถึงหมวดหมู่
                    .Where(oi => oi.Order != null && oi.Order.Status == "COMPLETED")
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(oi => oi.Order!.BranchId == branchId.Value);
                }

                var topCategories = await query
                    .Where(oi => oi.Product != null && oi.Product.Category != null)
                    .GroupBy(oi => new { oi.Product!.CategoryId, oi.Product.Category!.Name })
                    .Select(g => new TopCategoryDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.Name,
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.SubTotal)
                    })
                    .OrderByDescending(c => c.TotalRevenue) // จัดอันดับตาม "ยอดเงินที่ทำได้"
                    .Take(top)
                    .ToListAsync();

                reply.Data = topCategories;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetTopCategoriesAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        public async Task<ResultListReply> GetTopStaffAsync(string interval = "monthly", int top = 5, int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                var nowThailand = DateTime.UtcNow.AddHours(7);
                DateTime startDateThai; 

                // เช็คเงื่อนไขว่าหน้าบ้านอยากได้ดูเป็นรายวัน หรือ รายเดือน
                if (interval.ToLower() == "daily")
                {
                    startDateThai = nowThailand.Date; 
                }
                else
                {
                    // Default เป็น monthly 
                    startDateThai = new DateTime(nowThailand.Year, nowThailand.Month, 1).Date;
                }

                // แปลงกลับเป็น UTC าไปเทียบใน Database
                var startDateUTC = DateTime.SpecifyKind(startDateThai.AddHours(-7), DateTimeKind.Utc);

                var query = _context.Orders
                    .Include(o => o.Employee)
                    .Where(o => o.CreatedAt >= startDateUTC && o.Status == "COMPLETED")
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(o => o.BranchId == branchId.Value);
                }

                // จัดกลุ่มตามพนักงาน และหาผลรวม
                var topStaff = await query
                    .GroupBy(o => new { o.EmployeeId, o.Employee!.FirstName, o.Employee.LastName })
                    .Select(g => new TopStaffDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                        TotalRevenue = g.Sum(o => o.NetAmount),
                        TotalOrders = g.Count()
                    })
                    .OrderByDescending(s => s.TotalRevenue) 
                    .Take(top)
                    .ToListAsync();

                reply.Data = topStaff;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetTopStaffAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred.";
            }
            return reply;
        }

        //  สรุปผลงานแต่ละสาขา (Branch Performance)
        public async Task<ResultListReply> GetBranchPerformanceAsync()
        {
            var reply = new ResultListReply();
            try
            {
                //  กำหนดเวลาเริ่มของวันนี้ (UTC)
                var startOfTodayThai = DateTime.UtcNow.AddHours(7).Date;
                var startOfTodayUTC = startOfTodayThai.AddHours(-7);

                // ดึงข้อมูลสาขาทั้งหมด
                var branches = await _context.Branches.ToListAsync();

                //  ดึงบิลทั้งหมดของวันนี้มาพักไว้ใน Memory (เพื่อ Performance ที่ดีกว่าการ Query ซ้ำๆ)
                var todayOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= startOfTodayUTC && o.Status == "COMPLETED")
                    .ToListAsync();

                var branchPerformances = branches.Select(b =>
                {
                    var branchOrders = todayOrders.Where(o => o.BranchId == b.Id).ToList();

                    return new BranchPerformanceDto
                    {
                        BranchId = b.Id,
                        BranchName = b.BranchName,
                        Status = b.IsActive ? "ACTIVE" : "INACTIVE",
                        TodaySales = branchOrders.Sum(o => o.NetAmount),
                        TodayOrders = branchOrders.Count,
                        CashSales = branchOrders.Where(o => o.PaymentMethod == "CASH").Sum(o => o.NetAmount),
                        TransferSales = branchOrders.Where(o => o.PaymentMethod == "TRANSFER").Sum(o => o.NetAmount)
                    };
                })
                .OrderByDescending(b => b.TodaySales) 
                .ToList();

                reply.Data = branchPerformances;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetBranchPerformanceAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while fetching branch performance.";
            }
            return reply;
        }

        //  ติดตามสถานะกะที่กำลังเปิด (Active Shifts Tracking)
        public async Task<ResultListReply> GetActiveShiftsPerformanceAsync(int? branchId = null)
        {
            var reply = new ResultListReply();
            try
            {
                // 1. ดึงกะทั้งหมดที่มีสถานะ "OPEN"
                var shiftsQuery = _context.POSShifts
                    .Include(s => s.Branch)
                    .Include(s => s.Employee)
                    .Where(s => s.Status == "OPEN")
                    .AsQueryable();

                if (branchId.HasValue && branchId.Value > 0)
                {
                    shiftsQuery = shiftsQuery.Where(s => s.BranchId == branchId.Value);
                }

                var activeShifts = await shiftsQuery.ToListAsync();
                var activeShiftIds = activeShifts.Select(s => s.Id).ToList();

                // ถ้าไม่มีกะเปิดอยู่เลย ส่งลิสต์ว่างกลับไป
                if (!activeShiftIds.Any())
                {
                    reply.Data = new List<ActiveShiftPerformanceDto>();
                    reply.Result.ToSuccessStatus("200");
                    reply.ToSuccessStatus();
                    return reply;
                }

                //  ดึงบิลที่เกิดขึ้น "ภายในกะที่เปิดอยู่" เหล่านี้
                var shiftOrders = await _context.Orders
                    .Where(o => o.POSShiftId.HasValue && activeShiftIds.Contains(o.POSShiftId.Value) && o.Status == "COMPLETED")
                    .ToListAsync();

                // คำนวณเงินลิ้นชัก
                var activeShiftPerformances = activeShifts.Select(s =>
                {
                    var ordersInThisShift = shiftOrders.Where(o => o.POSShiftId == s.Id).ToList();

                    // ยอดขายทั้งหมดในกะ รวมทั้งเงินสดและโอน
                    decimal currentShiftSales = ordersInThisShift.Sum(o => o.NetAmount);

                    // ยอดขายเฉพาะ "เงินสด" เพื่อเอาไปคำนวณเงินที่ต้องมีในลิ้นชักจริงๆ
                    decimal currentCashSales = ordersInThisShift.Where(o => o.PaymentMethod == "CASH").Sum(o => o.NetAmount);

                    return new ActiveShiftPerformanceDto
                    {
                        ShiftId = s.Id,
                        ShiftNumber = $"SHIFT-{s.Id:D4}", // สร้างหมายเลขกะให้ดูง่าย เช่น SHIFT-0012
                        BranchId = s.BranchId,
                        BranchName = s.Branch?.BranchName ?? "Unknown",
                        EmployeeName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : "Unknown",
                        OpenedAt = s.StartTime,
                        StartingCash = s.StartingCash,
                        CurrentShiftSales = currentShiftSales,
                        ExpectedDrawerCash = s.StartingCash + currentCashSales, // ลิ้นชัก = ทอนตั้งต้น + ขายเงินสด
                        Status = s.Status
                    };
                })
                .OrderBy(s => s.BranchId).ThenBy(s => s.OpenedAt)
                .ToList();

                reply.Data = activeShiftPerformances;
                reply.Result.ToSuccessStatus("200");
                reply.ToSuccessStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetActiveShiftsPerformanceAsync");
                reply.Result.ToErrorStatus();
                reply.Data = _env.IsDevelopment() ? ex.Message : "An error occurred while fetching active shifts.";
            }
            return reply;
        }
    }
}