using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services Configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            new ContentResult { 
                StatusCode = 400, 
                Content = context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Ungültige Eingabedaten.",
                ContentType = "text/plain" 
            };
    });

// 2. Database: Use Connection Resiliency and Ensure Directory exists for SQLite on Cloud Run
var dbPath = "app.db";
builder.Services.AddDbContext<DataContext>(options => options.UseSqlite($"Data Source={dbPath}"));

// 3. Auth & Infrastructure
builder.Services.AddScoped<EmailService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT_SECRET"] ?? "EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN")),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddCors(options => { options.AddPolicy("AllowAll", policy => policy.WithOrigins("https://ezzy2024.github.io").AllowAnyMethod().AllowAnyHeader().AllowCredentials()); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddScoped<ServiceApotheke.API.Services.InvoiceService>();
var app = builder.Build();

// 4. Ensure Database Schema on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    context.Database.EnsureCreated(); // Ensures table schema is present on Cloud Run
}

// 5. Middleware Pipeline
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

// 6. Cloud Run Port Binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");