namespace EduWalletApp.Services
{
    public interface IEmailNotificationService
    {
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);
    }
}
