using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class RegisterDto
    {
        public string Fname { get; set; } = null!;
        public string Lname { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
