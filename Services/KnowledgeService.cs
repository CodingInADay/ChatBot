using Chatbot.Data;
using Chatbot.Models;
using Chatbot.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chatbot.Services
{
    public class KnowledgeService
    {
        private readonly ChatbotContext _context;
        private readonly TextNormalizationService _normalizationService;

        public KnowledgeService(ChatbotContext context, TextNormalizationService normalizationService)
        {
            _context = context;
            _normalizationService = normalizationService;
        }

        public int AddKnowledge(string content, string keywords, string synonyms, bool isAiGenerated)
        {
            content = _normalizationService.NormalizeText(content);
            keywords = _normalizationService.NormalizeText(keywords);
            synonyms = _normalizationService.NormalizeText(synonyms);

            var knowledge = new Knowledge
            {
                Content = content,
                Keywords = keywords,
                Synonyms = synonyms,
                IsAiGenerated = isAiGenerated ? 1 : 0,
                Rating = 0,
                Timestamp = DateTime.UtcNow
            };

            _context.Knowledge.Add(knowledge);
            _context.SaveChanges();

            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i++)
            {
                _context.WordRelations.Add(new WordRelation
                {
                    Word = words[i],
                    NextWord = words[i + 1]
                });
            }
            _context.SaveChanges();

            return knowledge.Id;
        }

        public SearchResultViewModel SearchAnswer(string query, string lang, List<int> usedIds)
        {
            query = _normalizationService.NormalizeText(query);
            if (string.IsNullOrEmpty(query))
            {
                return new SearchResultViewModel
                {
                    Answer = lang == "fa" ? "پرس‌وجو خالی است!" : "Query is empty!",
                    ShowFeedback = false,
                    KnowledgeId = null
                };
            }

            var queryLower = query.ToLower();
            var results = new List<Knowledge>();

            foreach (var knowledge in _context.Knowledge)
            {
                if (usedIds.Contains(knowledge.Id)) continue;

                var keywordPhrases = knowledge.Keywords.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
                var synonyms = knowledge.Synonyms?.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();
                var synonymMap = new Dictionary<string, string>();
                foreach (var syn in synonyms)
                {
                    var parts = syn.Split('=');
                    if (parts.Length == 2) synonymMap[parts[0].Trim()] = parts[1].Trim();
                }

                bool allKeywordsMatch = true;
                foreach (var phrase in keywordPhrases)
                {
                    var phraseLower = phrase.ToLower();
                    bool phraseMatched = queryLower.Contains(phraseLower);

                    if (!phraseMatched)
                    {
                        foreach (var synonym in synonymMap)
                        {
                            if (synonym.Key.ToLower() == phraseLower && queryLower.Contains(synonym.Value.ToLower()))
                            {
                                phraseMatched = true;
                                break;
                            }
                        }
                    }

                    if (!phraseMatched)
                    {
                        allKeywordsMatch = false;
                        break;
                    }
                }

                if (allKeywordsMatch) results.Add(knowledge);
            }

            results = results.OrderByDescending(k => k.Rating).ToList();
            if (results.Any())
            {
                return new SearchResultViewModel
                {
                    Answer = results[0].Content,
                    ShowFeedback = true,
                    KnowledgeId = results[0].Id,
                    MoreAvailable = results.Count > 1
                };
            }

            return new SearchResultViewModel
            {
                Answer = lang == "fa" ? "دانش کافی نیست!" : "Not enough knowledge!",
                    ShowFeedback = false,
                    KnowledgeId = null,
                    MoreAvailable = false
            };
        }

        public bool SubmitFeedback(int knowledgeId, double rating, string userType)
        {
            if (!_context.Knowledge.Any(k => k.Id == knowledgeId) || (rating != 1 && rating != -1))
                return false;

            double weight = userType == "admin" ? 1 : 0.2;
            double ratingValue = rating * weight;

            var knowledge = _context.Knowledge.Find(knowledgeId);
            if (knowledge == null) return false;
            knowledge.Rating += ratingValue;

            _context.Feedback.Add(new Feedback
            {
                KnowledgeId = knowledgeId,
                UserType = userType,
                Rating = ratingValue
            });

            _context.SaveChanges();
            return true;
        }

        public bool EditKnowledge(int id, string content, string keywords, string synonyms)
        {
            var knowledge = _context.Knowledge.Find(id);
            if (knowledge == null) return false;

            knowledge.Content = _normalizationService.NormalizeText(content);
            knowledge.Keywords = _normalizationService.NormalizeText(keywords);
            knowledge.Synonyms = _normalizationService.NormalizeText(synonyms);

            _context.SaveChanges();
            return true;
        }

        public bool DeleteKnowledge(int id)
        {
            var knowledge = _context.Knowledge.Find(id);
            if (knowledge == null) return false;

            _context.Feedback.RemoveRange(_context.Feedback.Where(f => f.KnowledgeId == id));
            _context.Knowledge.Remove(knowledge);
            _context.SaveChanges();
            return true;
        }
    }
}