using Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs
{
    public class UpdatePermissionDto
    {
        [Required(ErrorMessage = "UserName is required")]
        public int UserId { get; set; }
        public string Role { get; set; } = null!;
    }
}
