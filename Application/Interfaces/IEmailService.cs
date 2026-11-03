using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendAdminEmailAsync(EmailInfoDto job, bool isOrder = true);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null);
    }
}
