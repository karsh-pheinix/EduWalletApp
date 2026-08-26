namespace EduWalletApp.Services;

public interface IActivityLogger
{
    Task LogAsync(int userId, string action, string? details = null);
}