using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace ServiceApotheke.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var email = new MimeMessage();
            
            string senderName = _config["SmtpSettings:SenderName"] ?? "Service Apotheke";
            string senderEmail = _config["SmtpSettings:SenderEmail"] ?? "team@serviceapotheke.tech";

            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var smtp = new SmtpClient();
            
            string server = _config["SmtpSettings:Server"] ?? "smtp.ionos.de";
            int port = int.Parse(_config["SmtpSettings:Port"] ?? "587");
            string username = _config["SmtpSettings:Username"] ?? "";
            string password = _config["SmtpSettings:Password"] ?? "";

            // IONOS erfordert SecureSocketOptions.StartTls für externe Zugriffe über Port 587
            await smtp.ConnectAsync(server, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}