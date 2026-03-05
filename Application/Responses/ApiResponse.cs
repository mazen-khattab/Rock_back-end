using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Responses
{
    public class ApiResponse<T>
    {
        public bool isSucess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
