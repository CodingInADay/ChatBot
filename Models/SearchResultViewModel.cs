namespace Chatbot.Models.ViewModels
{
    // مدل نمایشی برای نتایج جستجو
    public class SearchResultViewModel
    {
        public required string Answer { get; set; } // پاسخ
        public bool ShowFeedback { get; set; } // نمایش دکمه‌های بازخورد
        public int? KnowledgeId { get; set; } // شناسه دانش
        public bool MoreAvailable { get; set; } // وجود پاسخ‌های بیشتر
    }
}