using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Settings
{
    public class SmtpSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpLogin { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
    }
}
