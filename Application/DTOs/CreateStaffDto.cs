using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CreateStaffDto : CreateCustomerDto
    {
        [Required, MinLength(1), MaxLength(2)]
        public List<string> Roles { get; set; } = new();
    }
}
