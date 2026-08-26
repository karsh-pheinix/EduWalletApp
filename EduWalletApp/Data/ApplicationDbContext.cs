using EduWalletApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<IncomeSource> IncomeSources { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<BudgetAllocation> BudgetAllocations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BankStatement> BankStatements { get; set; }
        public DbSet<StatementTransaction> StatementTransactions { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<FinancialTip> FinancialTips { get; set; }
        public DbSet<LocationService> LocationServices { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Feedback> Feedback { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<FinancialTip>().HasOne(t => t.TargetCategory).WithMany()
                .HasForeignKey(t => t.TargetCategoryId).OnDelete(DeleteBehavior.SetNull);

            // --- Decimal precision ---
            modelBuilder.Entity<IncomeSource>().Property(i => i.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Budget>().Property(b => b.SavingsGoalPercentage).HasPrecision(5, 2);
            modelBuilder.Entity<BudgetAllocation>().Property(b => b.BudgetedAmount).HasColumnName("Amount").HasPrecision(18, 2);
            modelBuilder.Entity<BudgetAllocation>().Property(b => b.SuggestedCap).HasPrecision(18, 2);
            modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<StatementTransaction>().Property(t => t.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Report>().Property(r => r.TotalIncome).HasPrecision(18, 2);
            modelBuilder.Entity<Report>().Property(r => r.TotalSavings).HasPrecision(18, 2);
            modelBuilder.Entity<Report>().Property(r => r.TotalMandatoryExpenses).HasPrecision(18, 2);
            modelBuilder.Entity<Report>().Property(r => r.TotalDisposableIncome).HasPrecision(18, 2);
            modelBuilder.Entity<Report>().Property(r => r.TotalActualSpend).HasPrecision(18, 2);

            // --- Cascade-delete conflict prevention ---
            // Transaction reaches User both directly (UserId) and indirectly
            // through BudgetAllocation -> Budget -> User. Restrict the
            // indirect path so only one cascade route exists.
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.BudgetAllocation)
                .WithMany(ba => ba.Transactions)
                .HasForeignKey(t => t.BudgetAllocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // BankStatement reaches User both directly (UserId) and
            // indirectly through Account -> User. Restrict the indirect path.
            modelBuilder.Entity<BankStatement>()
                .HasOne(bs => bs.Account)
                .WithMany(a => a.BankStatements)
                .HasForeignKey(bs => bs.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report reaches User both directly (UserId) and indirectly
            // through Budget -> User. Restrict the indirect path.
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Budget)
                .WithMany(b => b.Reports)
                .HasForeignKey(r => r.BudgetId)
                .OnDelete(DeleteBehavior.Restrict);

            // StatementTransaction's only path to User is through
            // BankStatement -> User — single path, no action needed, but
            // Category/SubCategory are nullable here (unparsed rows), so
            // their default cascade behavior is fine as-is.
        }
    }
}