using Application.Interfaces;
using Core.Extensions;
using Hangfire;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundJobs
{
    public class OutboxProcessor : IOutboxProcessor
    {
        private readonly AppDbContext _context;
        private readonly IEnumerable<IOutboxMessageStrategy> _strategies;
        private readonly ILogger<OutboxProcessor> _logger;

        public OutboxProcessor(
            AppDbContext context,
            IEnumerable<IOutboxMessageStrategy> strategies,
            ILogger<OutboxProcessor> logger)
        {
            _context = context;
            _strategies = strategies;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        [AutomaticRetry(Attempts = 0, LogEvents = false)]
        public async Task ProcessPendingMessagesAsync()
        {
            var pendingMessages = await _context.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync();

            if (!pendingMessages.Any())
                return;

            foreach (var message in pendingMessages)
            {
                try
                {
                    var messageType = message.Type.ToOutboxMessageType();
                    var strategy = _strategies.FirstOrDefault(s => s.MessageType == messageType);

                    if (strategy == null)
                    {
                        throw new InvalidOperationException($"No strategy registered for outbox message type: '{message.Type}'.");
                    }

                    await strategy.ProcessAsync(message.Payload);

                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = null;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                    _logger.LogError(ex, "Failed to process OutboxMessage Id: {Id}, Type: {Type}, RetryCount: {RetryCount}",
                        message.Id, message.Type, message.RetryCount);
                }

                // Immediately commit this message's outcome so earlier successes are never re-sent
                await _context.SaveChangesAsync();
            }
        }
    }
}
