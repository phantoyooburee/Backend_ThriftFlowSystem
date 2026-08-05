namespace Backend_ThriftFlowSystem.DTOs
{
    public class DashboardSummaryDto
    {
        public decimal TodaySales { get; set; }
        public int TodayTotalOrders { get; set; }
        public int PendingSlipsCount { get; set; }
        public int LowStockItemsCount { get; set; }
    }

    public class SalesChartDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal CashSales { get; set; }
        public decimal TransferSales { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class TopPerformerDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class InventoryAlertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }
        public decimal SellingPrice { get; set; }
        public string AlertType { get; set; } = string.Empty; // "LOW_STOCK" หรือ "DEADSTOCK"
        public int DaysInStock { get; set; }
    }

    public class RecentTransactionDto
    {
        public int OrderId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal NetAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
    }

    public class LotPerformanceDto
    {
        public int LotId { get; set; }
        public string LotName { get; set; } = string.Empty;
        public int TotalReceived { get; set; }

        public int TotalProcessed { get; set; }

        public int TotalSold { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal? TotalCost { get; set; }
        public decimal? Profit { get; set; }

    }

    public class TopCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopStaffDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }

    public class BranchPerformanceDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public decimal TodaySales { get; set; }          // ยอดขายรวมวันนี้ของสาขานี้
        public int TodayOrders { get; set; }             // จำนวนบิลวันนี้
        public decimal CashSales { get; set; }           // ยอดขายเงินสดวันนี้
        public decimal TransferSales { get; set; }       // ยอดขายสแกนโอนวันนี้
        public string Status { get; set; } = "ACTIVE";   // สถานะสาขา
    }

    public class ActiveShiftPerformanceDto
    {
        public int ShiftId { get; set; }
        public string ShiftNumber { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty; // ชื่อแคชเชียร์/ผู้เปิดกะ
        public DateTime OpenedAt { get; set; }                   // เวลาเปิดกะ
        public decimal StartingCash { get; set; }                // เงินทอนเริ่มต้น
        public decimal CurrentShiftSales { get; set; }           // ยอดขายสะสมในกะนี้
        public decimal ExpectedDrawerCash { get; set; }          // เงินที่ควรมีในลิ้นชัก (Starting + Cash Sales)
        public string Status { get; set; } = "OPEN";             
    }

}