using Application.DTOs;
using Application.Interfaces;
using Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
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
            var adminEmail = !string.IsNullOrWhiteSpace(_smtpSettings.AdminEmail)
                ? _smtpSettings.AdminEmail
                : "mazenkhtab11@gmail.com";

            string subject;
            var builder = new BodyBuilder();

            if (isOrder)
            {
                subject = $"New order from {job.FName} {job.LName}".Trim();
                builder.HtmlBody = "<p>Attached is the customer order in PDF format.</p>";
                builder.TextBody = "Attached is the customer order in PDF format.";

                byte[] pdfBytes = CreateOrderPdf(job);
                builder.Attachments.Add("OrderDetails.pdf", pdfBytes, new ContentType("application", "pdf"));
            }
            else
            {
                subject = "New contact Submission from Website";
                builder.HtmlBody = $@"<h2>User Contact Info</h2>
                    <p><strong>Name:</strong> {job.FName}</p>
                    <p><strong>Phone Number:</strong> {job.Phone}</p>";
                builder.TextBody = $"User Contact Info\nName: {job.FName}\nPhone Number: {job.Phone}";
            }

            await SendEmailInternalAsync(adminEmail, subject, builder);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null)
        {
            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                builder.Attachments.Add(attachmentFileName, attachmentBytes);
            }

            await SendEmailInternalAsync(toEmail, subject, builder);
        }

        private async Task SendEmailInternalAsync(string toEmail, string subject, BodyBuilder bodyBuilder)
        {
            var senderEmail = !string.IsNullOrWhiteSpace(_smtpSettings.SenderEmail)
                ? _smtpSettings.SenderEmail
                : (!string.IsNullOrWhiteSpace(_smtpSettings.SmtpLogin) ? _smtpSettings.SmtpLogin : "mazenkhtab123@gmail.com");

            var senderDisplayName = !string.IsNullOrWhiteSpace(_smtpSettings.SenderDisplayName)
                ? _smtpSettings.SenderDisplayName
                : "Rock Store";

            _logger.LogInformation("Sending email to {Recipient} from {Sender} with subject '{Subject}'", toEmail, senderEmail, subject);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderDisplayName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = bodyBuilder.ToMessageBody();

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

                _logger.LogInformation("Email sent successfully to {Recipient}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}: {Message}", toEmail, ex.Message);
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
                587 => SecureSocketOptions.StartTls,
                25 => SecureSocketOptions.StartTlsWhenAvailable,
                _ => SecureSocketOptions.Auto
            };
        }

        private byte[] CreateOrderPdf(EmailInfoDto job)
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);

                    page.Header().Row(row =>
                    {
                        var image = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Rock_logo.jpg");
                        if (File.Exists(image))
                        {
                            row.ConstantItem(70).Image(image);
                        }

                        row.RelativeItem().AlignCenter().Text("Rock Store - Order Summary")
                            .FontColor(Colors.Green.Darken4)
                            .FontSize(22)
                            .ExtraBold();
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // user info section
                        col.Item().PaddingTop(40).Text("User Info:")
                             .FontSize(16)
                             .Bold()
                             .FontColor(Colors.Green.Darken4);

                        // name
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(60).Text("Name: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.FName} {job.LName}");
                        });

                        // email
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(60).Text("Email: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.Email}");
                        });

                        // phone
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(60).Text("Phone: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.Phone}");
                        });

                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Grey.Medium);

                        // address section
                        col.Item().PaddingTop(10).Text("Address:")
                             .FontSize(16)
                             .Bold()
                             .FontColor(Colors.Green.Darken4);

                        // Governorate
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(100).Text("Governorate: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.Governorate}");
                        });

                        // City
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(100).Text("City: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.City}");
                        });

                        // Address
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(100).Text("Address: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.Address}");
                        });

                        // Notes
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(100).Text("Notes: ").ExtraBold();
                            row.ConstantItem(350).Text($"{job.Notes}");
                        });

                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(10).Text("Order Items:")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Green.Darken4);

                        foreach (var item in job.Items)
                        {
                            col.Item().Row(row =>
                            {
                                row.ConstantItem(30).Text($"{item.Quantity}").FontSize(15).Bold();
                                row.AutoItem().Text($"{item.Name}  -  {item.Size}  -  {item.Color}").FontSize(14);
                                row.ConstantItem(30).Text("").FontSize(14);
                                row.ConstantItem(30).Text("*").FontSize(14);
                                row.AutoItem().Text($"{item.Price}   = ").FontSize(14);
                                row.RelativeItem().AlignRight().Text($"{item.Price * item.Quantity}").FontSize(14);
                            });
                        }

                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        col.Item().AlignRight().Text($"Total Price: {job.TotalPrice}").Bold();
                    });

                    page.Footer().AlignCenter()
                        .Text("Thank you for shopping at Rock")
                        .FontSize(14)
                        .FontColor(Colors.Green.Darken4);
                });
            });

            return pdf.GeneratePdf();
        }
    }
}
