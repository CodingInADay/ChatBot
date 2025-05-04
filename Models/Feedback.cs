namespace Chatbot.Models
{
    // مدل بازخورد برای جدول feedback
    public class Feedback
    {
        public int KnowledgeId { get; set; } // شناسه دانش مرتبط
        public required string UserType { get; set; } // نوع کاربر (admin یا guest)
        public double Rating { get; set; } // امتیاز بازخورد
    }
}