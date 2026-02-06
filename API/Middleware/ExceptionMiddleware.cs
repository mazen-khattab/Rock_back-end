using Core.ExceptionsTypes;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Middleware
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
                _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}, Method: {Method}", context.Request.Path, context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, exception.Message),
                ValidationException => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
                _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
            };

            context.Response.StatusCode = (int)statusCode;

            var errorResponse = new
            {
                StatusCode = (int)statusCode,
                Message = message,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier,
                Details = _env.IsDevelopment() ? exception.StackTrace : null,
                InnerException = _env.IsDevelopment() ? exception.InnerException?.Message : null

            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var errorJson = JsonSerializer.Serialize(errorResponse);

            return context.Response.WriteAsync(errorJson);
        }
    }
}
