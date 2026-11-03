namespace Core.Settings
{
    public class SmtpSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpLogin { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = "Rock Store";
        public string AdminEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
