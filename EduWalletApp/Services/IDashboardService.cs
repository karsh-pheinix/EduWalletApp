using EduWalletApp.DTOs;

namespace EduWalletApp.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetUserDashboardSummaryAsync(int userId);
    }
}