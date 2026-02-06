using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
