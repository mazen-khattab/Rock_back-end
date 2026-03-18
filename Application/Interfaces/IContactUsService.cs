using Application.DTOs;
using Application.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IContactUsService
    {
        ApiResponse<string> ContactUs(ContactUsDto contactUs);
    }
}
