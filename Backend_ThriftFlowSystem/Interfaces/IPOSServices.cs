using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IPOSServices
    {
        Task<ResultListReply> CheckoutAsync(CheckoutRequest request, int employeeId);
        Task<ResultListReply> UploadSlipLaterAsync(int orderId, IFormFile slipImage, int employeeId);
        Task<ResultListReply> CalculateCartAsync(CalculateCartRequest request);
        Task<ResultListReply> SearchOrderByReceiptAsync(string receiptNumber);
        Task<ResultListReply> ProcessRefundAsync(RefundRequestDto request, int employeeId);
        Task<ResultListReply> OpenShiftAsync(int employeeId, OpenShiftRequest request);
        Task<ResultListReply> CloseShiftAsync(int shiftId, int employeeId, CloseShiftRequest request);
        Task<ResultListReply> AddCashTransactionAsync(int branchId, int employeeId, CashTransactionRequest request);
        Task<ResultListReply> GetActiveShiftAsync(int branchId);
    }
}