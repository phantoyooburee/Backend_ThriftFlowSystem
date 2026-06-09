namespace Backend_ThriftFlowSystem.Interfaces
{
    public class ResetPasswordEmail
    {
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
    public interface IEmailServices
    {
        Task SendEmailAsync(ResetPasswordEmail email);
    }
}
