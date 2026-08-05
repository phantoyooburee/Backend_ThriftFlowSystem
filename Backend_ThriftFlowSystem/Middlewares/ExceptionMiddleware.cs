using System.Net;
using System.Text.Json;

namespace Backend_ThriftFlowSystem.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex, _env);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, IWebHostEnvironment env)
        {
            // เช็คก่อนว่า Response เริ่มส่งไปหา Client หรือยัง
            if (context.Response.HasStarted)
            {
                // ถ้าเริ่มส่งไปแล้ว เราไม่สามารถแก้ Header/StatusCode ได้แล้ว ให้ return ออกไปเลย
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                Result = new { Code = "500", Message = "Error", Description = "An unexpected error occurred." },
                Data = env.IsDevelopment() ? exception.Message : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}