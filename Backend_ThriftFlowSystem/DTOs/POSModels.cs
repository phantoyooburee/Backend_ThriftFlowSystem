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

        public decimal? SpecialPrice { get; set; }

        public bool SkipPromotion { get; set; } = false;

        public IFormFile? SlipImage { get; set; } // allow null for cash payments

        [Required]
        public string OrderItemsJson { get; set; } = string.Empty;
        public string? ManagerPin { get; set; }
    }
}