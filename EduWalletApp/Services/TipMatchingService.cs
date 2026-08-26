using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EduWalletApp.Services;

public class TipMatchingService(ApplicationDbContext context, FinanceCalculator calculator) : ITipMatchingService
{
    public async Task<List<StudentTipDto>> GetRelevantTipsAsync(int userId)
    {
        // Do not load TargetCategory here: returning that navigation with the
        // tip entity can create a circular JSON graph for the student API.
        var tips = await context.FinancialTips.AsNoTracking().ToListAsync();
        var budget = await context.Budgets.Include(b => b.BudgetAllocations).ThenInclude(a => a.Transactions)
            .Where(b => b.UserId == userId).OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync();
        var statementRows = new List<(int? CategoryId, decimal Amount)>();
        if (budget is not null)
        {
            var storedStatementRows = await context.StatementTransactions
                .Where(st => st.BankStatement.UserId == userId
                    && st.Date >= budget.StartDate
                    && st.Date <= budget.EndDate
                    && st.Amount < 0)
                .Select(st => new { st.CategoryId, st.Amount })
                .ToListAsync();
            statementRows = storedStatementRows.Select(row => (row.CategoryId, Amount: -row.Amount)).ToList();
        }
        var statementSpendByCategory = statementRows
            .Where(row => row.CategoryId.HasValue)
            .GroupBy(row => row.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Amount));
        var result = new List<(FinancialTip Tip, int Severity)>();

        if (budget is not null)
        {
            foreach (var allocation in budget.BudgetAllocations)
            {
                var manualSpend = allocation.Transactions
                    .Where(t => t.UserId == userId && t.Date >= budget.StartDate && t.Date <= budget.EndDate)
                    .Sum(t => t.Amount);
                var spent = manualSpend + statementSpendByCategory.GetValueOrDefault(allocation.CategoryId);
                var ratio = allocation.BudgetedAmount > 0 ? spent / allocation.BudgetedAmount * 100 : 0;
                result.AddRange(tips.Where(t => t.TriggerType == TipTriggerType.OverBudget && ratio >= (t.ThresholdPercentage ?? 100) && (!t.TargetCategoryId.HasValue || t.TargetCategoryId == allocation.CategoryId)).Select(t => (t, 4)));
            }
            var incomeSources = await context.IncomeSources.Where(i => i.UserId == userId && i.IsActive).ToListAsync();
            var income = incomeSources.Sum(i => calculator.ToMonthlyEquivalent(i.Amount, i.Frequency));
            var budgetManualSpend = await context.Transactions
                .Where(t => t.UserId == userId && t.Date >= budget.StartDate && t.Date <= budget.EndDate)
                .SumAsync(t => t.Amount);
            var statementSpend = statementRows.Sum(row => row.Amount);
            var spend = budgetManualSpend + statementSpend;
            result.AddRange(tips.Where(t => t.TriggerType == TipTriggerType.NegativeDisposableIncome && spend > income).Select(t => (t, 3)));
            var savingsRate = income > 0 ? Math.Max(0, (income - spend) / income * 100) : 0;
            result.AddRange(tips.Where(t => t.TriggerType == TipTriggerType.LowSavings && savingsRate < (budget.SavingsGoalPercentage)).Select(t => (t, 2)));
        }

        result.AddRange(tips.Where(t => t.TriggerType == TipTriggerType.General).Select(t => (t, 1)));
        return result.OrderByDescending(item => item.Severity)
            .ThenByDescending(item => item.Tip.PublishedAt)
            .Select(item => item.Tip)
            .DistinctBy(tip => tip.Id)
            .Take(3)
            .Select(tip => new StudentTipDto(tip.Id, tip.Title, tip.Content, tip.Category, tip.PublishedAt))
            .ToList();
    }
}