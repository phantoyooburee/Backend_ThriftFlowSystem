using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IPOSServices
    {
        Task<ResultListReply> CheckoutAsync(CheckoutRequest request, int employeeId);
    }
}