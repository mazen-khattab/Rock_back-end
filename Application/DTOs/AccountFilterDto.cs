using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class AccountFilterDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 12;

        [StringLength(100)]
        public string? Search { get; set; }
    }
}
