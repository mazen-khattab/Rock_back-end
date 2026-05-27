using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class ProductDetailsVariantImagesAdminDto
    {
        public IFormFile File { get; set; } = null!;
        public int sortOrder { get; set; }
    }
}
