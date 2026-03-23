using Application.DTOs;
using Application.Interfaces;
using Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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


        [HttpPost]
        [Route("ContactUs")]
        public IActionResult ContactUs([FromBody] ContactUsDto contactUs)
        {
            _logger.LogInformation("Calling contactUs endpoint from: {userName}", contactUs.Name);

            var response = _contactUsService.ContactUs(contactUs);
            
            if (!response.IsSucess)
            {
                _logger.LogWarning("ContactUs service returned failure for {Phone}: {Message}",
                    contactUs.Phone, response.Message);

                return BadRequest(response);
            }

            _logger.LogInformation("ContactUs request processed successfully for {Phone}", contactUs.Phone);
            return Ok(response);
        }
    }
}
