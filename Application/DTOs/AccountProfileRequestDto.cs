using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class AccountProfileRequestDto
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string Governorate { get; set; } = string.Empty;

        [StringLength(200)]
        public string City { get; set; } = string.Empty;

        [StringLength(1000)]
        public string FullAddress { get; set; } = string.Empty;
    }
}
