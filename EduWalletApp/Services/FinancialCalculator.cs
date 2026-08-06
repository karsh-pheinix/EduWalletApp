using EduWalletApp.Models;

namespace EduWalletApp.Services
{
    public class FinanceCalculator
    {
        // Converts any income/expense frequency into its monthly equivalent.
        // Daily uses ×22 working days, per spec.
        public decimal ToMonthlyEquivalent(decimal amount, IncomeFrequency frequency)
        {
            return frequency switch
            {
                IncomeFrequency.Monthly => amount,
                IncomeFrequency.Weekly => amount * 4,
                IncomeFrequency.Daily => amount * 22,
                IncomeFrequency.OnceOff => amount,
                _ => amount
            };
        }

        public decimal ToMonthlyEquivalent(decimal amount, ExpenseFrequency frequency)
        {
            return frequency switch
            {
                ExpenseFrequency.Monthly => amount,
                ExpenseFrequency.Daily => amount * 22,
                ExpenseFrequency.OnceOff => amount,
                _ => amount
            };
        }

        // Disposable = Total Income − Savings Goal − Mandatory Expenses
        public decimal CalculateDisposableIncome(
            IEnumerable<IncomeSource> incomes,
            IEnumerable<Expense> mandatoryExpenses,
            decimal savingsGoalPercentage)
        {
            var totalIncome = incomes.Sum(i => ToMonthlyEquivalent(i.Amount, i.Frequency));
            var savingsAmount = totalIncome * (savingsGoalPercentage / 100);
            var totalMandatory = mandatoryExpenses.Sum(e => ToMonthlyEquivalent(e.Amount, e.Frequency));

            return totalIncome - savingsAmount - totalMandatory;
        }

        // Under-review check: expense > 20% of disposable income
        public bool IsUnderReview(decimal expenseAmount, decimal disposableIncome)
        {
            if (disposableIncome <= 0) return true;
            return (expenseAmount / disposableIncome) > 0.20m;
        }

        public decimal SuggestedCap(decimal disposableIncome) => disposableIncome * 0.20m;

        // Verdict mapping: ≤10% → Wise, 10–20% → Caution, >20% → Reconsider
        public string GetVerdict(decimal expenseAmount, decimal disposableIncome)
        {
            if (disposableIncome <= 0) return "Reconsider";

            var ratio = expenseAmount / disposableIncome;

            if (ratio <= 0.10m) return "Wise";
            if (ratio <= 0.20m) return "Caution";
            return "Reconsider";
        }
    }
}