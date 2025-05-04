namespace Chatbot.Models
{
    // مدل کاربر برای جدول users
    public class User
    {
        public int Id { get; set; } // شناسه یکتا
        public required string Username { get; set; } // نام کاربری
        public required string Password { get; set; } // رمز عبور (هش‌شده)
    }
}