using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class ContactUsService : IContactUsService
    {
        private readonly ILogger<ContactUsService> _logger;

        public ContactUsService(ILogger<ContactUsService> logger)
        {
            _logger = logger;
        }

        public ApiResponse<string> ContactUs(ContactUsDto contactUs)
        {
            _logger.LogInformation("Contact us from: {userName}, Phone: {Phone}", contactUs.Name, contactUs.Phone);

            try
            {
                BackgroundJob.Enqueue<IEmailService>(e => e.SendAdminEmailAsync(new EmailInfoDto
                {
                    FName = contactUs.Name,
                    Phone = contactUs.Phone
                }, false));

                return new ApiResponse<string>
                {
                    isSucess = true,
                    Message = "Message sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue contact us job for: {Phone}", contactUs.Phone);

                return new ApiResponse<string>
                {
                    isSucess = false,
                    Message = "Failed to process your request. Please try again later.",
                };
            }
        }
    }
}
