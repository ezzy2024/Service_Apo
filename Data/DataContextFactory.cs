using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceApotheke.API.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            
            // Nutzen Sie SQLite nur, wenn Sie offline Migrationen erstellen
            // Ansonsten sollte hier eine Verbindung zu einer lokalen Test-DB stehen
            optionsBuilder.UseSqlite("Data Source=design-time.db");

            return new DataContext(optionsBuilder.Options);
        }
    }
}