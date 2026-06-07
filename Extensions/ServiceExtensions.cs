using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;
using ServiceApotheke.API.Middleware;

namespace ServiceApotheke.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            var connString = config.GetConnectionString("DefaultConnection");
            services.AddDbContext<DataContext>(options => {
                if (string.IsNullOrEmpty(connString)) 
                    options.UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), "app.db")}");
                else 
                    options.UseNpgsql(connString);
            });
            services.AddScoped<EmailService>();
            services.AddScoped<InvoiceService>();
            return services;
        }

        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            // Ihre JWT Logik hier...
            return services;
        }

        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionMiddleware>();
        }

        public static void MigrateDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            context.Database.EnsureCreated();
        }
    }
}