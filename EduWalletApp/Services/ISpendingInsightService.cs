using EduWalletApp.DTOs;

namespace EduWalletApp.Services
{
    public interface ISpendingInsightService
    {
        Task<SpendingInsightDto> GetInsightsForStatementAsync(int statementId, int userId);
    }
}
