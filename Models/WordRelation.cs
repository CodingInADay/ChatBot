namespace Chatbot.Models
{
    // مدل روابط کلمات برای جدول word_relations
    public class WordRelation
    {
        public required string Word { get; set; } // کلمه فعلی
        public required string NextWord { get; set; } // کلمه بعدی
    }
}