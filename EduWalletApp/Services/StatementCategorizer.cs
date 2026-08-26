using EduWalletApp.Models;

namespace EduWalletApp.Services
{
    public static class StatementCategorizer
    {
        private static readonly Dictionary<string, string> KeywordToCategory =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["uber"] = "Transport",
                ["bolt"] = "Transport",
                ["taxi"] = "Transport",
                ["gautrain"] = "Transport",
                ["shoprite"] = "Groceries",
                ["pick n pay"] = "Groceries",
                ["checkers"] = "Groceries",
                ["woolworths"] = "Groceries",
                ["spar"] = "Groceries",
                ["kfc"] = "Food",
                ["mcdonalds"] = "Food",
                ["nandos"] = "Food",
                ["steers"] = "Food",
                ["cafeteria"] = "Food",
                ["restaurant"] = "Food",
                ["takeout"] = "Food",
                ["netflix"] = "Entertainment",
                ["showmax"] = "Entertainment",
                ["spotify"] = "Entertainment",
                ["cinema"] = "Entertainment",
                ["vodacom"] = "Airtime & Data",
                ["mtn"] = "Airtime & Data",
                ["cell c"] = "Airtime & Data",
                ["telkom"] = "Airtime & Data",
                ["airtime"] = "Airtime & Data",
                ["data bundle"] = "Airtime & Data",
                ["bookstore"] = "Education",
                ["textbook"] = "Education",
                ["tuition"] = "Education",
                ["rent"] = "Rent",
                ["residence"] = "Rent",
            };

        public static (int? CategoryId, int? SubCategoryId) Match(string description, List<Category> categories)
        {
            if (string.IsNullOrWhiteSpace(description)) return (null, null);

            foreach (var category in categories)
            {
                if (description.Contains(category.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var subMatch = category.SubCategories
                        .FirstOrDefault(sc => description.Contains(sc.Name, StringComparison.OrdinalIgnoreCase));
                    return (category.Id, subMatch?.Id);
                }
            }

            foreach (var (keyword, canonicalName) in KeywordToCategory)
            {
                if (description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var match = categories.FirstOrDefault(c =>
                        c.Name.Equals(canonicalName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return (match.Id, null);
                }
            }

            return (null, null);
        }
    }
}
