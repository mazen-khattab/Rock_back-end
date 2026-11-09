using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly ILogger<ContactUsController> _logger;
        private readonly IContactUsService _contactUsService;

        public ContactUsController(ILogger<ContactUsController> logger, IContactUsService contactUsService)
        {
            _logger = logger;
            _contactUsService = contactUsService;
        }

        [HttpPost("ContactUs")]
        public IActionResult ContactUs([FromBody] ContactUsDto contactUs)
        {
            _logger.LogInformation("Contact request received");
            return this.ToApiResult(_contactUsService.ContactUs(contactUs));
        }
    }
}
