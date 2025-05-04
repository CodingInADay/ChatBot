using Chatbot.Data;
using Chatbot.Models;
using Chatbot.Models.ViewModels;
using Chatbot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatbot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ChatbotContext _context;
        private readonly TextNormalizationService _normalizationService;
        private readonly KnowledgeService _knowledgeService;
        private readonly SentenceGenerationService _sentenceGenerationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ChatbotContext context, TextNormalizationService normalizationService,
            KnowledgeService knowledgeService, SentenceGenerationService sentenceGenerationService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _normalizationService = normalizationService;
            _knowledgeService = knowledgeService;
            _sentenceGenerationService = sentenceGenerationService;
            _logger = logger;
        }

        public IActionResult Index(string lang = "fa", int page = 1, string search = "", string sort = "timestamp", string order = "DESC")
        {
            _logger.LogDebug("Index - Session LoggedIn: {LoggedIn}", HttpContext.Session.GetString("LoggedIn"));

            Response.Headers["Content-Type"] = "text/html; charset=utf-8";
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            bool isLoggedIn = HttpContext.Session.GetString("LoggedIn") == "true";
            lang = lang == "en" ? "en" : "fa";

            int perPage = 5;
            int offset = (page - 1) * perPage;
            var query = _context.Knowledge.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                search = _normalizationService.NormalizeText(search);
                query = query.Where(k => k.Content.Contains(search) || k.Keywords.Contains(search));
            }

            query = sort switch
            {
                "id" => order == "ASC" ? query.OrderBy(k => k.Id) : query.OrderByDescending(k => k.Id),
                "content" => order == "ASC" ? query.OrderBy(k => k.Content) : query.OrderByDescending(k => k.Content),
                "keywords" => order == "ASC" ? query.OrderBy(k => k.Keywords) : query.OrderByDescending(k => k.Keywords),
                "rating" => order == "ASC" ? query.OrderBy(k => k.Rating) : query.OrderByDescending(k => k.Rating),
                "is_ai_generated" => order == "ASC" ? query.OrderBy(k => k.IsAiGenerated) : query.OrderByDescending(k => k.IsAiGenerated),
                _ => order == "ASC" ? query.OrderBy(k => k.Timestamp) : query.OrderByDescending(k => k.Timestamp),
            };

            var knowledgeItems = query.Skip(offset).Take(perPage).ToList();
            int totalRecords = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / perPage);

            var stats = new
            {
                KnowledgeCount = _context.Knowledge.Count(),
                TokensCount = _context.WordRelations.Count()
            };

            var model = new
            {
                IsLoggedIn = isLoggedIn,
                Lang = lang,
                KnowledgeItems = knowledgeItems,
                Page = page,
                TotalPages = totalPages,
                Search = search,
                Sort = sort,
                Order = order,
                Stats = stats
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            _logger.LogDebug("Login - Username: {Username}, Password: {Password}", username, password);
            username = _normalizationService.NormalizeText(username ?? "");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                HttpContext.Session.SetString("LoggedIn", "true");
                _logger.LogInformation("Login - Session set to true for user: {Username}", username);
                return Content("success", "text/plain");
            }
            _logger.LogWarning("Login - Failed: User not found or password incorrect for username: {Username}", username);
            return Content("error", "text/plain");
        }

        [HttpPost]
        public IActionResult ChangePass(string newPass)
        {
            if (string.IsNullOrEmpty(newPass)) return Content("empty", "text/plain");
            if (HttpContext.Session.GetString("LoggedIn") != "true") return Content("unauthorized", "text/plain");

            var user = _context.Users.FirstOrDefault(u => u.Username == "admin");
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPass);
                _context.SaveChanges();
                return Content("success", "text/plain");
            }
            return Content("error", "text/plain");
        }

        [HttpPost]
        public IActionResult Train(TrainViewModel model)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true") return Content("unauthorized", "text/plain");
            if (string.IsNullOrEmpty(model.Sentence) || string.IsNullOrEmpty(model.Keywords))
                return Content("empty", "text/plain");

            _knowledgeService.AddKnowledge(model.Sentence, model.Keywords, model.Synonyms ?? "", model.IsAiGenerated);
            return Content("success", "text/plain");
        }

        [HttpPost]
        public IActionResult Query(string query, string usedIdsJson)
        {
            var usedIds = JsonSerializer.Deserialize<List<int>>(usedIdsJson ?? "[]") ?? new List<int>();
            var lang = Request.Query["lang"].ToString() == "en" ? "en" : "fa";
            var result = _knowledgeService.SearchAnswer(query, lang, usedIds);
            usedIds.Add(result.KnowledgeId ?? 0);

            var html = $"<div class=\"chat-message bot\"><div class=\"bubble\">{result.Answer}";
            if (result.MoreAvailable)
                html += " <a href=\"#\" onclick=\"getMore(event)\">[...]</a>";
            html += "</div>";

            if (result.ShowFeedback && result.KnowledgeId.HasValue)
            {
                html += "<div class=\"feedback\">";
                html += $"<button data-tooltip=\"{(lang == "fa" ? "پاسخ خوب است" : "Good response")}\" onclick=\"submitFeedback(event, {result.KnowledgeId}, 1)\">✔</button>";
                html += $"<button data-tooltip=\"{(lang == "fa" ? "پاسخ نامناسب است" : "Inappropriate response")}\" onclick=\"submitFeedback(event, {result.KnowledgeId}, -1)\">✖</button>";
                html += "</div>";
            }
            html += "</div>";
            html += $"<input type=\"hidden\" id=\"usedIds\" value='{JsonSerializer.Serialize(usedIds)}'>";

            return Content(html, "text/html");
        }

        [HttpPost]
        public IActionResult More(string query, string usedIdsJson)
        {
            var usedIds = JsonSerializer.Deserialize<List<int>>(usedIdsJson ?? "[]") ?? new List<int>();
            var lang = Request.Query["lang"].ToString() == "en" ? "en" : "fa";
            var result = _knowledgeService.SearchAnswer(query, lang, usedIds);
            usedIds.Add(result.KnowledgeId ?? 0);

            var html = $"<div class=\"chat-message bot\"><div class=\"bubble\">{result.Answer}";
            if (result.MoreAvailable)
                html += " <a href=\"#\" onclick=\"getMore(event)\">[...]</a>";
            html += "</div>";

            if (result.ShowFeedback && result.KnowledgeId.HasValue)
            {
                html += "<div class=\"feedback\">";
                html += $"<button data-tooltip=\"{(lang == "fa" ? "پاسخ خوب است" : "Good response")}\" onclick=\"submitFeedback(event, {result.KnowledgeId}, 1)\">✔</button>";
                html += $"<button data-tooltip=\"{(lang == "fa" ? "پاسخ نامناسب است" : "Inappropriate response")}\" onclick=\"submitFeedback(event, {result.KnowledgeId}, -1)\">✖</button>";
                html += "</div>";
            }
            html += "</div>";
            html += $"<input type=\"hidden\" id=\"usedIds\" value='{JsonSerializer.Serialize(usedIds)}'>";

            return Content(html, "text/html");
        }

        [HttpPost]
        public IActionResult Feedback(int knowledgeId, int rating)
        {
            var userType = HttpContext.Session.GetString("LoggedIn") == "true" ? "admin" : "guest";
            var feedbackKey = $"{userType}_{knowledgeId}";
            var feedbackGiven = HttpContext.Session.GetString("FeedbackGiven")?.Split(',')?.ToList() ?? new List<string>();
            if (HttpContext.Session.GetString("LoggedIn") != "true" && feedbackGiven.Contains(feedbackKey))
                return Content("already_voted", "text/plain");

            if (!_knowledgeService.SubmitFeedback(knowledgeId, rating, userType))
                return Content("invalid", "text/plain");

            if (userType == "guest")
            {
                feedbackGiven.Add(feedbackKey);
                HttpContext.Session.SetString("FeedbackGiven", string.Join(",", feedbackGiven));
            }

            return Content("success", "text/plain");
        }

        [HttpPost]
        public IActionResult Generate(string startPhrase)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true") return Content("unauthorized", "text/plain");
            var sentence = _sentenceGenerationService.GenerateSentence(startPhrase);
            return Content(JsonSerializer.Serialize(new { sentence }), "text/plain");
        }

        [HttpPost]
        public IActionResult Approve(string sentence)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true") return Content("unauthorized", "text/plain");
            if (string.IsNullOrEmpty(sentence)) return Content("empty", "text/plain");

            _knowledgeService.AddKnowledge(sentence, "", "", true);
            return Content("success", "text/plain");
        }

        [HttpPost]
        public IActionResult Edit(int id, TrainViewModel model)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true") return Content("unauthorized", "text/plain");
            if (string.IsNullOrEmpty(model.Sentence) || string.IsNullOrEmpty(model.Keywords))
                return Content("empty", "text/plain");

            if (_knowledgeService.EditKnowledge(id, model.Sentence, model.Keywords, model.Synonyms ?? ""))
            {
                _logger.LogInformation("Knowledge updated: ID {Id}", id);
                return Content("success", "text/plain");
            }
            return Content("error", "text/plain");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true") return Content("unauthorized", "text/plain");
            if (_knowledgeService.DeleteKnowledge(id))
            {
                _logger.LogInformation("Knowledge deleted: ID {Id}", id);
                return Content("success", "text/plain");
            }
            return Content("error", "text/plain");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Content("success", "text/plain");
        }
    }
}