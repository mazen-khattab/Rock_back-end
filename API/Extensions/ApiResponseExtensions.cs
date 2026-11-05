using Application.Responses;
using Microsoft.AspNetCore.Mvc;

namespace API.Extensions
{
    public static class ApiResponseExtensions
    {
        public static IActionResult ToApiResult<T>(this ControllerBase controller, ApiResponse<T> response)
        {
            if (response.IsSuccess)
                return controller.Ok(response);

            int statusCode = response.ErrorType.ToHttpStatusCode();
            var context = controller.HttpContext;
            return controller.StatusCode(statusCode, new ApiErrorResponse
            {
                StatusCode = statusCode,
                Message = response.Message,
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier
            });
        }
    }
}
