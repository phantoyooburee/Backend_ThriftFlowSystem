using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IGetSalesHistoryServices
    {
        Task<ResultListReply> GetSalesHistoryAsync(SalesHistoryQueryDto request);
        Task<ResultListReply> GetOrderDetailByIdAsync(int orderId, int currentEmployeeId, string? role);
    }
}
