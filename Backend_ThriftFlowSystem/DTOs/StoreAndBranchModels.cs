namespace Backend_ThriftFlowSystem.DTOs
{
    public class StoreProfileDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
        public string ReceiptFooter { get; set; } = string.Empty;
    }

    public class BranchDto
    {
        public string BranchName { get; set; } = string.Empty;
        public string? LocationDetails { get; set; }
    }
}
