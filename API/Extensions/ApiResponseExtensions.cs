using Application.Responses;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Extensions
{
    public static class ApiResponseExtensions
    {
        public static IActionResult ToApiResult<T>(this ControllerBase controller, ApiResponse<T>? response,
            Func<ApiResponse<T>, IActionResult>? onSuccess = null, int? failureStatusCode = null)
        {
            if (response?.IsSuccess == true)
                return onSuccess == null ? controller.Ok(response) : onSuccess(response);

            // A missing service result is a server error; an unclassified failure must never return 200.
            int statusCode = response == null ? StatusCodes.Status500InternalServerError
                : failureStatusCode ?? (response.ErrorType == Core.Enum.ErrorType.None
                    ? StatusCodes.Status400BadRequest : response.ErrorType.ToHttpStatusCode());
            var context = controller.HttpContext;
            return controller.StatusCode(statusCode, new ApiErrorResponse
            {
                StatusCode = statusCode,
                Message = response?.Message ?? "The service returned no response.",
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier
            });
        }

        public static IActionResult ToPagedApiResult<T>(this ControllerBase controller,
            ApiResponse<(IEnumerable<T> Items, int TotalCount)>? response, int pageNumber = 1, int pageSize = 12) =>
            controller.ToApiResult(response, result => controller.Ok(new PagedResultDto<T>
            {
                Items = result.Data.Items.ToList(),
                TotalCount = result.Data.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            }));

        public static IActionResult ToApiResult<T>(this ControllerBase controller, AuthServiceResponse? response,
            T? data = default, int failureStatusCode = StatusCodes.Status400BadRequest) =>
            controller.ToApiResult(response == null ? null : new ApiResponse<T>
            {
                IsSuccess = response.IsSuccess,
                Message = response.Message,
                Data = response.IsSuccess ? data : default
            }, failureStatusCode: failureStatusCode);
    }
}
