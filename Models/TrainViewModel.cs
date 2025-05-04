namespace Chatbot.Models.ViewModels
{
    // مدل نمایشی برای فرم آموزش سیستم
    public class TrainViewModel
    {
        public required string Sentence { get; set; } // جمله
        public required string Keywords { get; set; } // کلمات کلیدی
        public string? Synonyms { get; set; } // مترادف‌ها
        public bool IsAiGenerated { get; set; } // آیا توسط AI تولید شده است
    }
}