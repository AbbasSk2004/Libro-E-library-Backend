using System.Net;
using System.Net.Mail;
using System.Text;

namespace E_Library.API.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationCode);
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationCode)
        {
            var subject = "Verify Your Email - Libro Library";
            var body = GenerateVerificationEmailBody(userName, verificationCode);

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername) || 
                    string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("Email configuration is incomplete");
                    return false;
                }

                using var client = new SmtpClient(smtpServer, smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;
                message.BodyEncoding = Encoding.UTF8;

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }

        private string GenerateVerificationEmailBody(string userName, string verificationCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification - Libro Library</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }}
        .container {{
            background-color: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo {{
            font-size: 28px;
            font-weight: bold;
            color: #4f46e5;
            margin-bottom: 10px;
        }}
        .title {{
            font-size: 24px;
            color: #1f2937;
            margin-bottom: 20px;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .verification-code {{
            display: inline-block;
            background-color: #f3f4f6;
            border: 2px solid #4f46e5;
            color: #1f2937;
            padding: 20px 30px;
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 8px;
            border-radius: 8px;
            margin: 20px 0;
            text-align: center;
            font-family: 'Courier New', monospace;
        }}
        .code-container {{
            text-align: center;
            margin: 30px 0;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            color: #6b7280;
            font-size: 14px;
        }}
        .warning {{
            background-color: #fef3c7;
            border: 1px solid #f59e0b;
            border-radius: 6px;
            padding: 15px;
            margin: 20px 0;
            color: #92400e;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>📚 Libro Library</div>
            <h1 class='title'>Verify Your Email Address</h1>
        </div>
        
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            
            <p>Welcome to Libro Library! We're excited to have you join our community of book lovers.</p>
            
            <p>To complete your registration and start exploring our vast collection of books, please verify your email address using the verification code below:</p>
            
            <div class='code-container'>
                <div class='verification-code'>{verificationCode}</div>
            </div>
            
            <div class='warning'>
                <strong>Important:</strong> This verification code will expire in 24 hours for security reasons. If you don't verify your email within this time, you'll need to register again.
            </div>
            
            <p>Enter this code in the verification form on our website to complete your registration.</p>
            
            <p>Once verified, you'll be able to:</p>
            <ul>
                <li>Browse our extensive book collection</li>
                <li>Borrow books from our library</li>
                <li>Track your borrowed books</li>
                <li>Access your personal reading history</li>
            </ul>
        </div>
        
        <div class='footer'>
            <p>If you didn't create an account with Libro Library, please ignore this email.</p>
            <p>This email was sent from a notification-only address that cannot accept incoming email. Please do not reply to this message.</p>
            <p>&copy; 2024 Libro Library. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
