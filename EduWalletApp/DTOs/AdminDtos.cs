namespace EduWalletApp.DTOs;

public record AdminSummaryDto(int TotalUsers, int ActiveUsers, int DisabledUsers, int FeedbackCount, int ActivityCount);

public record AdminUserDto(
    int Id,
    string? Email,
    string FirstName,
    string LastName,
    string StudentIdNumber,
    bool IsDisabled,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset CreatedAt);

public record AdminActivityDto(int Id, int UserId, string? Email, string Action, string? Details, DateTimeOffset CreatedAt);

public record AdminFeedbackDto(int Id, int UserId, string? Email, int Rating, string? Comment, DateTimeOffset CreatedAt);

public class UpdateUserStatusDto
{
    public bool Disabled { get; set; }
}