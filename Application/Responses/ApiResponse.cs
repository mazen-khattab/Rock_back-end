using Core.Enum;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Responses
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty; 
        public ErrorType ErrorType { get; set; } = ErrorType.None;
        public T? Data { get; set; }
    }
}
