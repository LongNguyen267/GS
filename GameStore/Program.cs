using GameStore.Models;
using GameStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm namespace này cho Authentication
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ Session vào ứng dụng
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian sống của session là 30 phút
    options.Cookie.HttpOnly = true; // Cookie chỉ có thể truy cập bằng HTTP, không phải JavaScript
    options.Cookie.IsEssential = true; // Đánh dấu cookie là thiết yếu để session hoạt động
});

// Thêm dịch vụ DbContext vào Dependency Injection
// Cấu hình để sử dụng SQL Server với chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<GameStoreDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GameStoreDBConnection")));

// Thêm Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập
        options.LogoutPath = "/Account/Logout"; // Đường dẫn đến trang đăng xuất
        options.AccessDeniedPath = "/Account/AccessDenied"; // Trang khi truy cập bị từ chối
    });

builder.Services.AddAuthorization(); // Thêm dịch vụ Authorization
builder.Services.AddSingleton<IVnPayService, VnPayService>();
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Thêm middleware Session vào HTTP request pipeline
app.UseSession();

app.UseRouting();

// Thêm middleware authentication và authorization
// Quan trọng: Phải đặt sau UseRouting() và trước MapControllerRoute()
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();