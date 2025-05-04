using Chatbot.Models;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Data
{
    // زمینه دیتابیس برای مدیریت ارتباط با SQLite
    public class ChatbotContext : DbContext
    {
        public ChatbotContext(DbContextOptions<ChatbotContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Knowledge> Knowledge { get; set; }
        public DbSet<WordRelation> WordRelations { get; set; }
        public DbSet<Feedback> Feedback { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // پیکربندی مدل‌ها
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<Knowledge>()
                .HasKey(k => k.Id);

            modelBuilder.Entity<WordRelation>()
                .HasKey(w => new { w.Word, w.NextWord });

            modelBuilder.Entity<Feedback>()
                .HasKey(f => new { f.KnowledgeId, f.UserType, f.Rating });
        }

        // اطمینان از استفاده از UTF-8
        public void EnsureDatabaseCreated()
        {
            Database.ExecuteSqlRaw("PRAGMA encoding = 'UTF-8';");
            Database.EnsureCreated();

            // ایجاد کاربر پیش‌فرض (ادمین) در صورت عدم وجود
            if (!Users.Any())
            {
                Users.Add(new User
                {
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin")
                });
                SaveChanges();
            }
        }
    }
}