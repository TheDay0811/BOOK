using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using BOOK.Data;
using BOOK.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// 1. CƠ SỞ DỮ LIỆU QUAN HỆ (SQL SERVER / RDBMS): Lưu thông sách, thẻ, phiếu mượn, phạt (ACID)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CƠ SỞ DỮ LIỆU NOSQL (MONGODB): Tự động khởi chạy & kết nối lưu trữ System Audit Logs, AI Chat
var rawMongoConn = builder.Configuration.GetConnectionString("MongoConnection") ?? "mongodb://127.0.0.1:27017";
var activeMongoConn = MongoRunnerService.EnsureRunning(rawMongoConn, builder.Environment.ContentRootPath);
var mongoDbName = builder.Configuration.GetValue<string>("MongoDatabaseName") ?? "LibraryDB";

var mongoClient = new MongoClient(activeMongoConn);
var mongoDatabase = mongoClient.GetDatabase(mongoDbName);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddSingleton<ILibraryMongoLogService, LibraryMongoLogService>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ILibraryChatService, GeminiService>();

// Cấu hình Authentication Cookie cho toàn bộ hệ thống
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // A. Khởi tạo & nạp dữ liệu cho SQL Server
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);

    // B. Khởi tạo & nạp dữ liệu mẫu cho MongoDB (Sẵn sàng demo cho Giảng viên trên MongoDB Compass)
    var mongoLogService = scope.ServiceProvider.GetRequiredService<ILibraryMongoLogService>();
    await MongoSeeder.SeedInitialLogsAsync(mongoLogService);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
