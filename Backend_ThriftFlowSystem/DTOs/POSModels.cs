using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Backend_ThriftFlowSystem.DTOs
{
    // Models for POS (Point of Sale) system one by one
    public class OrderItemRequestDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    // Model for Checkout Request from Frontend
    public class CheckoutRequest
    {
        [Required]
        public string PaymentMethod { get; set; } = "CASH";
        public decimal? CashReceived { get; set; }

        public decimal? SpecialPrice { get; set; }

        public bool SkipPromotion { get; set; } = false;

        public IFormFile? SlipImage { get; set; } // allow null for cash payments

        [Required]
        public string OrderItemsJson { get; set; } = string.Empty;
        public string? ManagerPin { get; set; }
        public int BranchId { get; set; }
    }

    public class UploadSlipRequest
    {
        [Required(ErrorMessage = "Order ID is required.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Please provide a slip image.")]
        public IFormFile SlipImage { get; set; } = null!;
    }

    public class CalculateCartRequest
    {
        [Required]
        public List<OrderItemRequestDto> Items { get; set; } = new();
        public bool SkipPromotion { get; set; } = false;
        public decimal? SpecialPrice { get; set; }
    }

    public class CartPreviewResponse
    {
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public List<int> AppliedPromotionIds { get; set; } = new();
        public List<CartItemPreview> Items { get; set; } = new();
    }

    public class CartItemPreview
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal FullLineTotal { get; set; }
        public decimal DiscountedLineTotal { get; set; }
        public decimal EffectiveUnitPrice { get; set; }
        public decimal EffectiveSubTotal { get; set; }
        public int? AppliedPromotionId { get; set; }
    }

    public class OpenShiftRequest
    {
        public int BranchId { get; set; }
        public decimal StartingCash { get; set; }
    }

    public class CloseShiftRequest
    {
        public decimal ActualCash { get; set; }
        public string? Remarks { get; set; }
    }

    public class CashTransactionRequest
    {
        [Required]
        public string TransactionType { get; set; } = string.Empty; // ส่งค่ามาเป็น "CASH_IN" หรือ "CASH_OUT"

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string? Remarks { get; set; }
        public string? ManagerPin { get; set; }
    }
    public class RefundRequestDto
    {
        [Required]
        public int OriginalOrderId { get; set; }

        [Required]
        public string ManagerPin { get; set; } = string.Empty;

        public string? Reason { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item must be refunded.")]
        public List<RefundItemDto> Items { get; set; } = new();
    }

    public class RefundItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}