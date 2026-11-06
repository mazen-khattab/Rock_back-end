namespace Application.DTOs
{
    public class ColorDto
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
