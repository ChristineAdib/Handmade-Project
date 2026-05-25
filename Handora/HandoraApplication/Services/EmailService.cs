using HandoraApplication.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace HandoraApplication.Services
{
    public sealed class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(string email, string otpCode, CancellationToken ct = default)
        {
            var subject = "Your OTP Verification Code";
            var body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Email Verification</h2>
                        <p>Thank you for registering with us!</p>
                        <p>Your OTP verification code is:</p>
                        <h1 style='color: #007bff; letter-spacing: 5px;'>{otpCode}</h1>
                        <p>This code will expire in 5 minutes.</p>
                        <p>If you did not request this code, please ignore this email.</p>
                        <hr>
                        <p style='color: #666; font-size: 12px;'>This is an automated message, please do not reply.</p>
                    </body>
                </html>";

            return await SendEmailAsync(email, subject, body, ct);
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string body, CancellationToken ct = default)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var smtpServer = smtpSettings["Server"];
                var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
                var senderEmail = smtpSettings["SenderEmail"];
                var senderPassword = smtpSettings["SenderPassword"];
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogError("SMTP settings are not configured properly.");
                    return false;
                }

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    client.Timeout = 10000;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage, ct);
                    _logger.LogInformation("Email sent successfully to {Email}", email);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                return false;
            }
        }
    }
}
