using EduWalletApp.Models;

namespace EduWalletApp.Services
{
    public class ArchetypeCalculator
    {
        public Archetype Calculate(List<Archetype> answerLeanings)
        {
            return answerLeanings
                .GroupBy(a => a)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
        }
    }
}