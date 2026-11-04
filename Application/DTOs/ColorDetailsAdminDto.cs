using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ColorDetailsAdminDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Arabic name is required.")]
        [StringLength(200)]
        public string NameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "English name is required.")]
        [StringLength(200)]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hex code is required.")]
        [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Hex code must contain # followed by six hexadecimal digits.")]
        public string HexCode { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
