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

            // Prefer the most specific matching subcategory, even when the
            // parent category name is not present in the statement text.
            foreach (var category in categories.OrderByDescending(c => c.Name.Length))
            {
                var subMatch = category.SubCategories
                    .OrderByDescending(sc => sc.Name.Length)
                    .FirstOrDefault(sc => description.Contains(sc.Name, StringComparison.OrdinalIgnoreCase));
                if (subMatch != null)
                {
                    return (category.Id, subMatch.Id);
                }
            }

            foreach (var category in categories.OrderByDescending(c => c.Name.Length))
            {
                if (description.Contains(category.Name, StringComparison.OrdinalIgnoreCase))
                    return (category.Id, null);
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
