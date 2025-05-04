using Chatbot.Data;
using Chatbot.Models;
using Chatbot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// افزودن سرویس‌های MVC
builder.Services.AddControllersWithViews();

// افزودن سشن با تنظیمات انعطاف‌پذیر برای تست
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // تغییر به Lax برای سازگاری با Chrome
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // موقتاً برای HTTP در توسعه
    options.Cookie.Name = ".AspNetCore.Session"; // نام صریح برای کوکی
});

// پیکربندی دیتابیس SQLite
builder.Services.AddDbContext<ChatbotContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ثبت سرویس‌ها
builder.Services.AddScoped<TextNormalizationService>();
builder.Services.AddScoped<KnowledgeService>();
builder.Services.AddScoped<SentenceGenerationService>();

// تنظیم انکودینگ پیش‌فرض به UTF-8
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
builder.Services.AddSingleton(new UTF8Encoding(false));

// افزودن لاگ برای دیباگ
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

var app = builder.Build();

// پیکربندی پایپ‌لاین HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // قبل از Authorization
app.UseAuthorization();

// تنظیم مسیرهای پیش‌فرض
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ایجاد دیتابیس و کاربر پیش‌فرض
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChatbotContext>();
    context.Database.EnsureCreated();

    var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");
    if (adminUser == null)
    {
        adminUser = new User
        {
            Username = "admin",
            Password = BCrypt.Net.BCrypt.HashPassword("admin")
        };
        context.Users.Add(adminUser);
    }
    else if (!BCrypt.Net.BCrypt.Verify("admin", adminUser.Password))
    {
        adminUser.Password = BCrypt.Net.BCrypt.HashPassword("admin");
        context.Update(adminUser);
    }
    context.SaveChanges();
}

app.Run();