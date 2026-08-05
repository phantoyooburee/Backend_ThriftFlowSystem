using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IPromotionServices
    {
        Task<ResultListReply> GetAllPromotionsAsync(bool onlyActive = false);
        Task<ResultListReply> CreatePromotionAsync(PromotionRequestDto request, int employeeId);
        Task<ResultListReply> UpdatePromotionAsync(int id, PromotionRequestDto request, int employeeId);
        Task<ResultListReply> TogglePromotionActiveAsync(int id, int employeeId);
    }
}