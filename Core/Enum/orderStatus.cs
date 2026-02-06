using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Enums
{
    public enum OrderStatus
    {
        Pending,
        Processing,
        Confirmed,
        Shipped,
        Delivered,
        Cancelled,
        Returned
    }
}
