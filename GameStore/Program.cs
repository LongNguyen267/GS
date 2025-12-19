using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameStore.Chat;
using GameStore.Models.Services;
var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database
builder.Services.AddDbContext<GameStoreDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GameStoreDBConnection")));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IEmailSender, EmailSender>();
// SignalR (Chat)
builder.Services.AddSignalR();

var app = builder.Build();

// --- 2. CẤU HÌNH PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// [QUAN TRỌNG] Dòng này giúp web load được CSS, JS, Ảnh trong thư mục wwwroot
// Nếu thiếu dòng này, web sẽ trắng trơn và vỡ giao diện
app.UseStaticFiles();

app.UseSession(); // Kích hoạt Session

app.UseRouting(); // Kích hoạt định tuyến

// Authentication & Authorization phải nằm SAU UseRouting và TRƯỚC MapControllerRoute
app.UseAuthentication();
app.UseAuthorization();

// --- 3. ĐỊNH TUYẾN (ROUTES) ---

// Route SEO cho sản phẩm (Slug)
app.MapControllerRoute(
    name: "product-details",
    pattern: "san-pham/{slug}-{id}",
    defaults: new { controller = "Products", action = "Details" });

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Route cho Chat SignalR
app.MapHub<GameStore.Chat.ChatHub>("/chatHub");

app.Run();