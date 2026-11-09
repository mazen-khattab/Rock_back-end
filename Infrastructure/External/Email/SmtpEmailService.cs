using Application.DTOs;
using Application.Interfaces;
using Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using QuestPDF.Fluent;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.External.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendAdminEmailAsync(EmailInfoDto job, bool isOrder = true)
        {
            if (string.IsNullOrWhiteSpace(_smtpSettings.AdminEmail))
            {
                throw new InvalidOperationException("SmtpSettings:AdminEmail is not configured. Admin notifications cannot be sent without a configured recipient.");
            }

            var adminEmail = _smtpSettings.AdminEmail;

            string subject;
            var builder = new BodyBuilder();
            string idempotencyKey;

            if (isOrder)
            {
                subject = $"New order from {job.FName} {job.LName}".Trim();
                builder.HtmlBody = "<p>Attached is the customer order in PDF format.</p>";
                builder.TextBody = "Attached is the customer order in PDF format.";

                byte[] pdfBytes = CreateOrderPdf(job);
                builder.Attachments.Add("OrderDetails.pdf", pdfBytes, new ContentType("application", "pdf"));

                // Deterministic idempotency key for order emails
                idempotencyKey = $"order-{job.OrderNumber}";
            }
            else
            {
                subject = "New contact Submission from Website";
                builder.HtmlBody = $@"<h2>User Contact Info</h2>
                    <p><strong>Name:</strong> {job.FName}</p>
                    <p><strong>Phone Number:</strong> {job.Phone}</p>";
                builder.TextBody = $"User Contact Info\nName: {job.FName}\nPhone Number: {job.Phone}";

                // Deterministic idempotency key for contact inquiries
                idempotencyKey = $"contact-{job.Phone}-{job.FName}";
            }

            await SendEmailInternalAsync(adminEmail, subject, builder, idempotencyKey);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null, string? idempotencyKey = null)
        {
            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                builder.Attachments.Add(attachmentFileName, attachmentBytes);
            }

            await SendEmailInternalAsync(toEmail, subject, builder, idempotencyKey);
        }

        private async Task SendEmailInternalAsync(string toEmail, string subject, BodyBuilder bodyBuilder, string? idempotencyKey = null)
        {
            var senderEmail = !string.IsNullOrWhiteSpace(_smtpSettings.SenderEmail)
                ? _smtpSettings.SenderEmail
                : _smtpSettings.SmtpLogin;

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException("Neither SmtpSettings:SenderEmail nor SmtpSettings:SmtpLogin is configured.");
            }

            var senderDisplayName = !string.IsNullOrWhiteSpace(_smtpSettings.SenderDisplayName)
                ? _smtpSettings.SenderDisplayName
                : "Rock Store";

            _logger.LogInformation("Sending email to {Recipient} from {Sender} with subject '{Subject}' [IdempotencyKey: {Key}]",
                toEmail, senderEmail, subject, idempotencyKey ?? "None");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderDisplayName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = bodyBuilder.ToMessageBody();

            // Set deterministic RFC 5322 Message-ID if idempotency key is provided
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var domain = senderEmail.Contains('@') ? senderEmail.Split('@').Last() : "rockstore.com";
                var sanitizedKey = Regex.Replace(idempotencyKey, @"[^a-zA-Z0-9_\-\.]", "-");
                message.MessageId = $"{sanitizedKey}@{domain}";
                message.Headers.Add("X-Correlation-ID", sanitizedKey);
            }

            if (!_smtpSettings.EnableSsl && !string.IsNullOrWhiteSpace(_smtpSettings.SmtpPassword))
            {
                throw new InvalidOperationException("Refusing to transmit SMTP credentials over an unencrypted connection. Please enable SSL/TLS in SmtpSettings.");
            }

            try
            {
                using var smtp = new SmtpClient();

                var secureSocketOptions = ResolveSecureSocketOptions();

                await smtp.ConnectAsync(_smtpSettings.SmtpServer, _smtpSettings.SmtpPort, secureSocketOptions);

                if (!string.IsNullOrWhiteSpace(_smtpSettings.SmtpLogin) && !string.IsNullOrWhiteSpace(_smtpSettings.SmtpPassword))
                {
                    await smtp.AuthenticateAsync(_smtpSettings.SmtpLogin, _smtpSettings.SmtpPassword);
                }

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Recipient} with MessageId {MessageId}.", toEmail, message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} with MessageId {MessageId}: {Message}", toEmail, message.MessageId, ex.Message);
                throw;
            }
        }

        private SecureSocketOptions ResolveSecureSocketOptions()
        {
            if (!_smtpSettings.EnableSsl)
            {
                return SecureSocketOptions.None;
            }

            return _smtpSettings.SmtpPort switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.StartTls
            };
        }

        private byte[] CreateOrderPdf(EmailInfoDto job)
        {
            _logger.LogInformation("Generating dark order PDF for order number: {OrderNumber}", job.OrderNumber);
            var document = new OrderPdfDocument(job);
            byte[] pdf = document.GeneratePdf();
            _logger.LogInformation("Generated order PDF for order number: {OrderNumber}, size: {ByteCount} bytes", job.OrderNumber, pdf.Length);
            return pdf;
        }
    }
}
