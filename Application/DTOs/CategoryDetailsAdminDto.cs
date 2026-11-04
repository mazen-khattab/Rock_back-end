using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CategoryDetailsAdminDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Arabic name is required.")]
        [StringLength(200)]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "English name is required.")]
        [StringLength(200)]
        public string NameEn { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
