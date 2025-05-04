using System.Text.RegularExpressions;

namespace Chatbot.Services
{
    // سرویس نرمال‌سازی متن
    public class TextNormalizationService
    {
        // نرمال‌سازی متن: حذف فضاهای اضافی و استانداردسازی
        public string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Trim(); // حذف فضاهای ابتدا و انتها
            return Regex.Replace(text, @"\s+", " "); // تبدیل چند فضا به یک فضا
        }
    }
}