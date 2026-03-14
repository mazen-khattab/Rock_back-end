using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendAdminEmailAsync(EmailInfoDto job);
    }
}
