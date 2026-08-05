using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public enum NotificationType
    {
        LeakageAlert = 0,
        MonthEndNudge = 1,
        ReviewFlag = 2,
        SavingsNudge = 3,
        General = 4
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public NotificationType Type { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}