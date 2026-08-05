namespace Backend_ThriftFlowSystem.DTOs
{
    namespace Backend_ThriftFlowSystem.DTOs
    {
        public class AuthLogResponse
        {
            public int Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string Action { get; set; } = string.Empty;
            public string? ActorName { get; set; }
            public string? EmployeeName { get; set; }
            public string? TargetEmail { get; set; }
            public string? IPAddress { get; set; }
            public string? UserAgent { get; set; }
            public string? Details { get; set; }
        }

        public class InventoryLogResponse
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string ProductSKU { get; set; } = string.Empty;
            public string ActionType { get; set; } = string.Empty;
            public int QuantityChanged { get; set; }
            public string? Note { get; set; }
        }

        public class SystemActionLogResponse
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public string ActionType { get; set; } = string.Empty;
            public string TargetTable { get; set; } = string.Empty;
            public int? TargetRecordId { get; set; }
            public string? Details { get; set; }
        }

        public class RefundLogResponse
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public int OrderId { get; set; }
            public string ReceiptNumber { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public int? ApprovedById { get; set; }
            public string ApprovedByName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal RefundAmount { get; set; }
            public string? Reason { get; set; }
        }
        public class POSShiftLogResponse
        {
            public int Id { get; set; }
            public int BranchId { get; set; }
            public string BranchName { get; set; } = string.Empty;
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public decimal StartingCash { get; set; }
            public decimal CashInAmount { get; set; }
            public decimal CashOutAmount { get; set; }
            public decimal ExpectedCash { get; set; }
            public decimal ActualCash { get; set; }
            public decimal Difference { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string? Remarks { get; set; }
        }

        public class LogQueryRequest
        {
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public int? EmployeeId { get; set; }
            public int Page { get; set; } = 1;
            public string? Action { get; set; }
            public string? TargetTable { get; set; }   
            public string? SearchKeyword { get; set; } 
            public string? Status { get; set; }
            public int PageSize { get; set; } = 20;
        }

        public class PagedLogResponse<T>
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public int CurrentPage { get; set; }  
            public IEnumerable<T> Items { get; set; } = new List<T>();
        }
    }
}
