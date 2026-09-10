using Resend;

namespace EduWalletApp.Services
{
    public class ResendEmailNotificationService : IEmailNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResendEmailNotificationService> _logger;

        public ResendEmailNotificationService(
            IConfiguration configuration,
            ILogger<ResendEmailNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Resend:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Resend API key is missing. Email not sent to {Email}.", to);
                return;
            }

            var from = _configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";

            var resend = ResendClient.Create(apiKey);
            var response = await resend.EmailSendAsync(new EmailMessage
            {
                From = from,
                To = new[] { to },
                Subject = subject,
                HtmlBody = htmlBody
            }, cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation("Email sent successfully to {Email}.", to);
            }
            else
            {
                _logger.LogError("Failed to send email to {Email}: {Error}", to, response.Exception?.Message ?? "Unknown error");
            }
        }

        public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
        {
            await SendAsync(
                to: email,
                subject: "Reset your EduWallet password",
                htmlBody: $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #1f2937;'>
                        <h2>Password reset request</h2>
                        <p>Hello,</p>
                        <p>We received a request to reset your EduWallet password.</p>
                        <p>
                            <a href='{resetLink}' style='display: inline-block; background: #2563eb; color: white; padding: 12px 18px; border-radius: 8px; text-decoration: none;'>Reset my password</a>
                        </p>
                        <p>If you did not request this, you can ignore this email.</p>
                        <p>For security, this link expires soon.</p>
                    </div>
                ",
                cancellationToken: cancellationToken);
        }
    }
}
