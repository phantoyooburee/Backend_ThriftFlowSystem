using System;

namespace Backend_ThriftFlowSystem.DTOs
{
    public class SalesHistoryModels
    {
    }
    public class SalesHistoryQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20; 
        public string? SearchReceipt { get; set; } 
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? EmployeeId { get; set; } 
        public string? Status { get; set; } 
        public string? PaymentMethod { get; set; }
        public int? BranchId { get; set; }
        public int? POSShiftId { get; set; }
    }

    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PaymentSlipUrl { get; set; }
        public bool HasRefunds { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int? POSShiftId { get; set; }
    }

    public class PaginatedOrderResponseDto
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<OrderSummaryDto> Items { get; set; } = new List<OrderSummaryDto>();
    }
}
