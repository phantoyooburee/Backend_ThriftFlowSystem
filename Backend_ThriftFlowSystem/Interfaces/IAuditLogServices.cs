using Backend_ThriftFlowSystem.DTOs;
using Backend_ThriftFlowSystem.DTOs.Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IAuditLogServices
    {
        Task<ResultListReply> GetAuthLogsAsync(LogQueryRequest query);
        Task<ResultListReply> GetInventoryLogsAsync(LogQueryRequest query, string? actionType = null, int? productId = null);
        Task<ResultListReply> GetSystemActionLogsAsync(LogQueryRequest query, string? targetTable = null);
        Task<ResultListReply> GetRefundLogsAsync(LogQueryRequest query);
        Task<ResultListReply> GetPOSShiftLogsAsync(LogQueryRequest query, int? branchId = null);
    }
}
