using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Enum
{
    public enum ErrorType
    {
        None, // => 200
        InvalidCredentials, // => 401
        NotFound, // => 404
        InsufficientStock, // => 400
        ConcurrencyConflict, // => 409
        ValidationError, // => 422,
        ServerError, // => 500
    }
}
