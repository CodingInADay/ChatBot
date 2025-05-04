using System;

namespace Chatbot.Models
{
    // مدل دانش برای جدول knowledge
    public class Knowledge
    {
        public int Id { get; set; } // شناسه یکتا
        public required string Content { get; set; } // محتوای جمله
        public required string Keywords { get; set; } // کلمات کلیدی (نقطه‌جدا)
        public string? Synonyms { get; set; } // مترادف‌ها (با = و نقطه جدا)
        public int IsAiGenerated { get; set; } // آیا توسط AI تولید شده است (0 یا 1)
        public double Rating { get; set; } // امتیاز جمله
        public DateTime Timestamp { get; set; } // زمان ثبت
    }
}