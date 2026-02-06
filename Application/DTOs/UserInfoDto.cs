using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class UserInfoDto
    {
        public int? UserId { get; set; }
        public string UserName { get; set; } = null!;
        public List<string> Role { get; set; } = new List<string>();
    }
}
