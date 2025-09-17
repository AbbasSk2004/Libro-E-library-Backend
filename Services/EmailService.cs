using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

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
                var gmailUser = _configuration["EmailSettings:GmailUser"];
                if (string.IsNullOrWhiteSpace(gmailUser))
                {
                    _logger.LogError("Gmail user is not configured");
                    return false;
                }

                return await SendViaGmailApiAsync(gmailUser!, toEmail, subject, body, isHtml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }

        private async Task<bool> SendViaGmailApiAsync(string fromEmail, string toEmail, string subject, string body, bool isHtml)
        {
            try
            {
                var clientId = _configuration["EmailSettings:GoogleClientId"]!;
                var clientSecret = _configuration["EmailSettings:GoogleClientSecret"]!;
                var refreshToken = _configuration["EmailSettings:GmailRefreshToken"]!;

                var initializer = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    }
                };

                var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(initializer);
                var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
                {
                    RefreshToken = refreshToken
                };
                var credential = new UserCredential(flow, fromEmail, token);

                var service = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "E-Library API"
                });

                string mime;
                if (isHtml)
                {
                    mime = BuildMime(fromEmail, toEmail, subject, body, true);
                }
                else
                {
                    mime = BuildMime(fromEmail, toEmail, subject, body, false);
                }

                var raw = Base64UrlEncode(mime);
                var message = new Message { Raw = raw };
                await service.Users.Messages.Send(message, "me").ExecuteAsync();
                _logger.LogInformation($"Email sent successfully via Gmail API to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gmail API send failed");
                return false;
            }
        }

        private static string BuildMime(string from, string to, string subject, string body, bool isHtml)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"From: {from}");
            sb.AppendLine($"To: {to}");
            sb.AppendLine($"Subject: {subject}");
            sb.AppendLine("MIME-Version: 1.0");
            if (isHtml)
            {
                sb.AppendLine("Content-Type: text/html; charset=\"UTF-8\"");
            }
            else
            {
                sb.AppendLine("Content-Type: text/plain; charset=\"UTF-8\"");
            }
            sb.AppendLine();
            sb.Append(body);
            return sb.ToString();
        }

        private static string Base64UrlEncode(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
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
