using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IDashboardServices
    {
        Task<ResultListReply> GetDashboardSummaryAsync(int? branchId = null, string userRole = "Owner", int? currentEmployeeId = null);
        Task<ResultListReply> GetSalesAnalyticsAsync(string interval = "daily", int days = 7, int? branchId = null);
        Task<ResultListReply> GetTopPerformersAsync(int top = 5, int? branchId = null);
        Task<ResultListReply> GetInventoryAlertsAsync(int lowStockThreshold = 10, int deadstockDays = 30, int? top = null);
        Task<ResultListReply> GetRecentTransactionsAsync(int limit = 10, int? branchId = null);
        Task<ResultListReply> GetLotPerformanceAsync(string Userrole,int limit = 5);
        Task<ResultListReply> GetTopCategoriesAsync(int top = 5, int? branchId = null);
        Task<ResultListReply> GetTopStaffAsync(string interval = "monthly", int top = 5, int? branchId = null);
        Task<ResultListReply> GetBranchPerformanceAsync();
        Task<ResultListReply> GetActiveShiftsPerformanceAsync(int? branchId = null);
    }
}