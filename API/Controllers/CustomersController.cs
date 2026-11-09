using System.ComponentModel.DataAnnotations;
using API.Extensions;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Owner,Admin")]
    public class CustomersController : ControllerBase
    {
        private readonly IAccountManagementService _accountManagement;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(IAccountManagementService accountManagement, ILogger<CustomersController> logger)
        {
            _accountManagement = accountManagement;
            _logger = logger;
        }

        /// <summary>
        /// Lists User-only accounts, excluding anyone with Admin or Owner privileges.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListCustomers([FromQuery] AccountFilterDto filter)
        {
            _logger.LogInformation("Customer list requested by actor ID: {ActorId}", User.GetRequiredUserId());
            var result = await _accountManagement.ListCustomers(filter);
            return result.IsSuccess ? Ok(result.Data) : this.ToApiResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCustomerDetails([Range(1, int.MaxValue)] int id)
        {
            _logger.LogInformation("Customer details requested for ID: {AccountId}", id);
            return this.ToApiResult(await _accountManagement.GetCustomer(id));
        }

        /// <summary>
        /// Creates a customer with the User role; role selection is not accepted from the request.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            var result = await _accountManagement.CreateCustomer(dto, User.GetRequiredUserId());
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetCustomerDetails), new { id = result.Data!.Id }, result)
                : this.ToApiResult(result);
        }

        [HttpPut("{id:int}/edit")]
        public async Task<IActionResult> UpdateCustomer([Range(1, int.MaxValue)] int id, [FromBody] AccountProfileRequestDto dto) =>
            this.ToApiResult(await _accountManagement.UpdateCustomer(id, dto, User.GetRequiredUserId()));

        /// <summary>
        /// Permanently deletes a customer only when no active or deleted orders exist.
        /// </summary>
        [HttpDelete("{id:int}/delete")]
        public async Task<IActionResult> DeleteCustomer([Range(1, int.MaxValue)] int id) =>
            this.ToApiResult(await _accountManagement.DeleteCustomer(id, User.GetRequiredUserId()));
    }
}
