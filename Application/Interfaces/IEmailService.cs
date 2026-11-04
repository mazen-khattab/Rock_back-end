using Application.DTOs;
using Hangfire;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        [AutomaticRetry(Attempts = 3)]
        Task SendAdminEmailAsync(EmailInfoDto job, bool isOrder = true);

        [AutomaticRetry(Attempts = 3)]
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null, string? idempotencyKey = null);
    }
}
