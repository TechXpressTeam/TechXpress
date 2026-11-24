
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;
using TechXpress.Web.Services.Interfaces;

namespace TechXpress.Web.Services.Implementations
{
        public class EmailSender : IEmailSender
        {
            private readonly string _smtpServer;
            private readonly int _smtpPort;
            private readonly string _fromAddress;
            private readonly string _username;
            private readonly string? _password;

            public EmailSender(IConfiguration configuration)
            {
                _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
                _fromAddress = configuration["EmailSettings:FromAddress"] ?? throw new InvalidOperationException("EmailSettings:FromAddress is not configured.");
                _username = configuration["EmailSettings:Username"] ?? _fromAddress;
                _password = configuration["EmailSettings:Password"];
            }

            public async Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                if (string.IsNullOrWhiteSpace(_password))
                {
                    throw new InvalidOperationException("Email cannot be sent because EmailSettings:Password is not configured. Set the EmailSettings__Password environment variable to your Gmail App Password and restart the application.");
                }

                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_username, _password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromAddress),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                }
            }
        }
    }

