using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class UpdateStaffDto : AccountProfileRequestDto
    {
        // Omit roles to preserve them when editing only profile information.
        [MinLength(1), MaxLength(2)]
        public List<string>? Roles { get; set; }
    }
}
