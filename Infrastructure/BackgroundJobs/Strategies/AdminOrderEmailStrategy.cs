using Application.DTOs;
using Application.Interfaces;
using Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundJobs.Strategies
{
    public class AdminOrderEmailStrategy : IOutboxMessageStrategy
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminOrderEmailStrategy> _logger;

        public AdminOrderEmailStrategy(IEmailService emailService, ILogger<AdminOrderEmailStrategy> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public OutboxMessageType MessageType => OutboxMessageType.AdminOrderNotification;

        public async Task ProcessAsync(string payload)
        {
            var emailDto = JsonSerializer.Deserialize<EmailInfoDto>(payload);
            if (emailDto == null)
            {
                throw new InvalidOperationException("Failed to deserialize EmailInfoDto from outbox payload.");
            }

            await _emailService.SendAdminEmailAsync(emailDto);
        }
    }
}