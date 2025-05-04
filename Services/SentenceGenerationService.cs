using Chatbot.Data;
using Chatbot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Chatbot.Services
{
    // سرویس تولید جمله با Markov Chain
    public class SentenceGenerationService
    {
        private readonly ChatbotContext _context;
        private readonly TextNormalizationService _normalizationService;

        public SentenceGenerationService(ChatbotContext context, TextNormalizationService normalizationService)
        {
            _context = context;
            _normalizationService = normalizationService;
        }

        // تولید جمله با استفاده از عبارت اولیه
        public string GenerateSentence(string startPhrase)
        {
            if (string.IsNullOrEmpty(startPhrase))
                return "لطفاً یک عبارت اولیه وارد کنید.";

            var startWords = _normalizationService.NormalizeText(startPhrase).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentWord = startWords.Last();
            var sentence = startWords.ToList();

            for (int i = 0; i < 10; i++)
            {
                var nextWord = _context.WordRelations
                    .Where(w => w.Word == currentWord)
                    .OrderBy(r => Guid.NewGuid())
                    .Select(w => w.NextWord)
                    .FirstOrDefault();

                if (nextWord == null) break;

                sentence.Add(nextWord);
                currentWord = nextWord;
            }

            return string.Join(" ", sentence);
        }
    }
}