using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceApotheke.API.Data
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            
            // Use a local SQLite file for migration generation so it never needs remote connectivity
            optionsBuilder.UseSqlite("Data Source=design-time.db");

            return new DataContext(optionsBuilder.Options);
        }
    }
}