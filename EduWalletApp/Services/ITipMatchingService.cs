using EduWalletApp.DTOs;

namespace EduWalletApp.Services;

public interface ITipMatchingService
{
    Task<List<StudentTipDto>> GetRelevantTipsAsync(int userId);
}