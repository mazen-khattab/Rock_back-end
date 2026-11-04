using Core.Enums;
using System;

namespace Core.Extensions
{
    public static class OutboxMessageTypeExtensions
    {
        public static string ToDatabaseString(this OutboxMessageType value) => value switch
        {
            OutboxMessageType.AdminOrderNotification => "AdminOrderNotification",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown outbox type.")
        };

        public static OutboxMessageType ToOutboxMessageType(this string value) => value switch
        {
            "AdminOrderNotification" => OutboxMessageType.AdminOrderNotification,
            _ => throw new ArgumentException("Unknown database value for outbox type.", nameof(value))
        };
    }
}
