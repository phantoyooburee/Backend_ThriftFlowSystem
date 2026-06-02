using Backend_ThriftFlowSystem.DTOs;

namespace Backend_ThriftFlowSystem.Interfaces
{
    public interface IResultReplyServices
    {
        int MapReply(ResultListReply reply);
        Task<ErrorStatus?> ErrorMessage(int errorCode);
    }
}
