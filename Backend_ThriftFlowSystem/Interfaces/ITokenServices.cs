using Backend_ThriftFlowSystem.Models;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface ITokenServices
    {
        string GenerateJwtToken(Employee employee);
        string GenerateResetPasswordToken();
        string SHA256Hex(string input);
    }
}
