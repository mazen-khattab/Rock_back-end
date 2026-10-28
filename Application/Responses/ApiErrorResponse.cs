using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Responses
{
    public class ApiErrorResponse
    {
        public bool IsSuccess { get; set; } = false; // Always false in the exception
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;

        // Nullable to remove them in the production.
        public string? Details { get; set; }
        public string? InnerException { get; set; }
    }
}
