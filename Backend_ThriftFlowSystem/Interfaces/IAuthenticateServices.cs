using Backend_ThriftFlowSystem.DTOs;
using static Backend_ThriftFlowSystem.DTOs.AuthenticateModels;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IAuthenticateServices
    {
        Task<ResultListReply> SetupOwnerAsync(SetupOwnerRequest request);
        Task<ResultListReply> InviteEmployeeAsync(InviteEmployeeRequest request, int inviterId);
        Task<ResultListReply> RegisterAsync(RegisterRequest request);
        Task<ResultListReply> LoginAsync(LoginRequest request);
        Task<ResultListReply> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ResultListReply> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ResultListReply> LogoutAsync(int employeeId, string email);
    }
}
