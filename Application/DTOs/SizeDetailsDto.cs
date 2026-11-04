using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class SizeDetailsDto
    {
        [Required(ErrorMessage = "Size name is required.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
