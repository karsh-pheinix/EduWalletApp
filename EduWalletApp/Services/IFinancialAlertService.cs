namespace EduWalletApp.Services;

public interface IFinancialAlertService
{
    Task CheckAndCreateAlertsAsync(int userId);
}