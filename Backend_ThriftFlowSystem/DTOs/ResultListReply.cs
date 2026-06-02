
namespace Backend_ThriftFlowSystem.DTOs
{
    public enum ResultStatus
    {
        Success,
        Error
    }
    public class ResultListReply
    {
        public ActionResult Result { get; set; }
        public object? Data { get; set; }
        public ResultStatus? Status { get; set; }

        public ResultListReply()
        {
            this.Result = new ActionResult();
        }

        public void ToSuccessStatus()
        {
            Status = ResultStatus.Success;
        }

        public void ToErrorStatus()
        {
            Status = ResultStatus.Error;
        }
    }
    public class ResultListReplyPagging : ResultListReply
    {
        public int Count { get; set; }
    }

    public class ErrorStatus
    {
        public int ErrorCode { get; set; }
        public required string ErrorDesc { get; set; }
    }
}
