using ServiceApotheke.API.Data;
using ServiceApotheke.API.Extensions;
using ServiceApotheke.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using ServiceApotheke.API.Services;

// --- FAILSAFE: Verhindert Abstürze durch fehlende SMTP-Variablen ---
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMTP_PORT")))
{
    Environment.SetEnvironmentVariable("SMTP_PORT", "587");
    Environment.SetEnvironmentVariable("SMTP_HOST", "smtp.ethereal.email");
    Environment.SetEnvironmentVariable("SMTP_USER", "dummy");
    Environment.SetEnvironmentVariable("SMTP_PASS", "dummy");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiConfiguration(); // Extension für JSON & CORS
builder.Services.AddApplicationServices(builder.Configuration); // Extension für DB & Services
builder.Services.AddIdentityServices(builder.Configuration); // Extension für JWT
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

// WICHTIG: Die Datenbank im temporären, beschreibbaren Ordner von Cloud Run speichern
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // Fallback für lokale Entwicklung mit SQLite, falls keine Verbindungsvariable existiert
    var dbPath = Path.Combine(Path.GetTempPath(), "app.db");
    builder.Services.AddDbContext<DataContext>(options => options.UseSqlite($"Data Source={dbPath}"));
}
else
{
    // PostgreSQL für Produktion (Cloud Run)
    builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
}

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

// WICHTIG: Dynamische CORS-Richtlinie, um alle CORS-Fehler sicher auszuschließen
builder.Services.AddCors(options => { 
    options.AddPolicy("AllowAll", policy => 
        policy.WithOrigins("https://serviceapotheke.tech") // Hier Ihre Domain eintragen!
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()); 
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddScoped<ServiceApotheke.API.Services.InvoiceService>();

var app = builder.Build();
app.UseGlobalExceptionHandler(); // Eigene Middleware für Fehler
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

// --- GLOBALE FEHLERBEHANDLUNG ---
// Fängt Backend-Abstürze ab und sendet sie als lesbare JSON-Antwort an das Frontend
app.UseExceptionHandler(c => c.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    Console.WriteLine($"[CRITICAL ERROR] {exception?.Message}\n{exception?.StackTrace}");
    context.Response.StatusCode = 500;
    context.Response.ContentType = "application/json";
    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
    await context.Response.WriteAsJsonAsync(new { 
        message = "Ein interner Serverfehler ist aufgetreten.", 
        detail = exception?.Message 
    });
}));

// 4. Ensure Database Schema on Startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    
    // Fallback if using SQLite
    if (context.Database.IsSqlite())
    {
        context.Database.EnsureCreated();
    }
    else
    {
        // Force PostgreSQL to apply the latest schema
        try {
            context.Database.EnsureCreated(); // Ensure tables exist
        } catch (Exception ex) {
            Console.WriteLine($"DB Init Error: {ex.Message}");
        }
    }
}

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.MigrateDatabase();
app.UseAuthentication();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");