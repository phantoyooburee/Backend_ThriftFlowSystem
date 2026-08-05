using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IStoreServices
    {
        Task<ResultListReply> GetStoreProfileAsync();
        Task<ResultListReply> UpdateStoreProfileAsync(StoreProfileDto request, int employeeId);
        Task<ResultListReply> GetAllBranchesAsync();
        Task<ResultListReply> CreateBranchAsync(BranchDto request, int employeeId);
        Task<ResultListReply> UpdateBranchAsync(BranchDto request, int branchId, int employeeId);
        Task<ResultListReply> ToggleBranchActiveAsync(int branchId, int employeeId);
    }
}
