using System;
using System.Collections.Generic;
using System.Text;

namespace Core.ExceptionsTypes
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
