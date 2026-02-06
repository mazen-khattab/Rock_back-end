using System;
using System.Collections.Generic;
using System.Text;

namespace Core.ExceptionsTypes
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}
