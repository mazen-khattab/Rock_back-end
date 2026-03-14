using Core.Interfaces;
using Core.Settings;
using Microsoft.Extensions.Options;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Org.BouncyCastle.Asn1.X9;
using System;
using System.Collections.Generic;
using System.Text;
using MailKit.Net.Smtp;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.External.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SmtpEmailService> _logger;
        const string Email = "mazenkhtab123@gmail.com";
        public SmtpEmailService(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }


        public async Task SendAdminEmailAsync(EmailInfoDto job)
        {
            _logger.LogInformation("Sending an email to mazenkhtab11@gmail.com from: {sender}", Email);

            MimeMessage email = new();
            email.From.Add(new MailboxAddress("Rock Store", Email));
            //email.From.Add(new MailboxAddress('', senderEmail));
            email.To.Add(MailboxAddress.Parse("mazenkhtab11@gmail.com"));

            email.Subject = $"New order from {job.FName} {job.LName}";

            var builder = new BodyBuilder
            {
                HtmlBody = "<p>Attached is the customer order in PDF format.</p>"
            };

            byte[] pdfBytes = CreateOrderPdf(job);

            builder.Attachments.Add("OrderDetails.pdf", pdfBytes, ContentType.Parse("application/pdf"));
            email.Body = builder.ToMessageBody();

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_smtpSettings.SmtpServer, _smtpSettings.SmtpPort, false);
                await smtp.AuthenticateAsync(_smtpSettings.SmtpLogin, _smtpSettings.SmtpPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error sending email: {ex.Message}");
                throw;
            }
        }

        private byte[] CreateOrderPdf(EmailInfoDto job)
        {
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A4);
                    page.Background(Colors.White);

                    page.Header().Row(row =>
                    {
                        var image = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\Images", "Rock_logo.jpg");
                        row.ConstantItem(70).Image(image);

                        row.RelativeItem().AlignCenter().Text("Rock Store - Order Summary")
                            .FontColor(Colors.Green.Darken4)
                            .FontSize(22)
                            .ExtraBold();
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // usre info section
                        col.Item().PaddingTop(40).Text("User Info:")
                             .FontSize(16)
                             .Bold()
                             .FontColor(Colors.Green.Darken4);
                        //col.Item().Text($"Name: {job.FName} {job.LName}").FontSize(14);
                        //col.Item().Text($"Email: {job.Email}").FontSize(14);
                        //col.Item().Text($"Phone: {job.Phone}").FontSize(14);

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
                        //col.Item().Text($"Governorate: {job.Governorate}").FontSize(14);
                        //col.Item().Text($"City: {job.City}").FontSize(14);
                        //col.Item().Text($"Full Address: {job.Address}").FontSize(14);
                        //col.Item().Text($"Notes: {job.Notes}").FontSize(14);

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
                                //row.ConstantItem(30).Text("").FontSize(14);
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
