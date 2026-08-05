using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EduWalletApp.Models
{
    public enum Gender
    {
        Female = 0,
        Male = 1,
        Other = 2,
        PreferNotToSay = 3
    }

    public enum Archetype
    {
        Analyst = 0,
        Socialite = 1,
        ImpulseBuyer = 2,
        Avoider = 3
    }

    public class User : IdentityUser<int>
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string StudentIdNumber { get; set; } = string.Empty;

        [Required]
        public string University { get; set; } = string.Empty;

        [Required]
        public int YearOfStudy { get; set; }

        public Gender Gender { get; set; }

        public DateOnly DateOfBirth { get; set; }

        [Required]
        public string City { get; set; } = string.Empty;

        public Archetype? Archetype { get; set; }

        public int ArchetypeXP { get; set; } = 0;

        public bool OnboardingComplete { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<IncomeSource> IncomeSources { get; set; } = new List<IncomeSource>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<BankStatement> BankStatements { get; set; } = new List<BankStatement>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}