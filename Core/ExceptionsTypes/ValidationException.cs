using System;
using System.Collections.Generic;
using System.Text;

namespace Core.ExceptionsTypes
{
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
