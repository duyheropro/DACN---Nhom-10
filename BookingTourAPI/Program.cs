// Program.cs
using BookingTourAPI.Data;
using BookingTourAPI.Services;
using BookingTourAPI.Hubs;
using BookingTourAPI.Models;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================== ĐĂNG KÝ DỊCH VỤ ==================

// HttpClient gọi Amadeus
builder.Services.AddHttpClient<AmadeusService>();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Các service custom
builder.Services.AddScoped<AmadeusService>();
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Dùng JwtUserIdProvider cho SignalR
builder.Services.AddSingleton<IUserIdProvider, JwtUserIdProvider>();

// Service gửi email xác nhận / thông báo
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Service xác nhận đơn hàng (nếu bạn đang dùng)
builder.Services.AddScoped<IOrderConfirmationService, OrderConfirmationService>();

// Identity quản lý user + role
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Cấu hình xác thực JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Cho phép SignalR đọc token từ query string (dùng cho adminHub, userHub, chatHub)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/adminHub")
                 || path.StartsWithSegments("/userHub")
                 || path.StartsWithSegments("/chatHub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ================== BUILD APP ==================
var app = builder.Build();

// Seed data: tạo role, admin...
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedDataService.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// static files wwwroot
app.UseStaticFiles();

// static files cho AdminPortal (js, css)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "AdminPortal", "js")),
    RequestPath = "/js"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "AdminPortal", "css")),
    RequestPath = "/css"
});

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ================== MAP HTML ADMIN ==================

// trang login admin
app.Map("/login.html", (HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "AdminPortal", "login.html");
    return Results.File(filePath, "text/html");
});

// trang dashboard admin
app.Map("/dashboard.html", (HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "AdminPortal", "dashboard.html");
    return Results.File(filePath, "text/html");
});

// 👉 TRANG QUẢN LÝ ĐÁNH GIÁ (MỚI THÊM)
app.Map("/reviews.html", (HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "AdminPortal", "reviews.html");
    return Results.File(filePath, "text/html");
});

// (nếu có thêm reports.html hoặc trang khác, bạn map tương tự)

// redirect root -> login admin
app.Map("/", () => Results.Redirect("/login.html"));

// ================== MAP API & HUB ==================
app.MapControllers();

app.MapHub<AdminNotificationHub>("/adminHub");
app.MapHub<UserNotificationHub>("/userHub");
app.MapHub<ChatHub>("/chatHub");
app.MapHub<PublicHub>("/publicHub");


app.Map("/html/forgot-password.html", (HttpContext context) => {
    return Results.File(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "html", "forgot-password.html"), "text/html");
});

app.Map("/html/reset-password.html", (HttpContext context) => {
    return Results.File(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "html", "reset-password.html"), "text/html");
});

app.Run();
