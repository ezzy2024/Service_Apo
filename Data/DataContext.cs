using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Data
{
    public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    
    public DbSet<Pharmacist> Pharmacists { get; set; }
    public DbSet<JobPost> JobPosts { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<Pharmacy> Pharmacies { get; set; } // WICHTIG!

        // Neue Referenzen
        public DbSet<PharmacistFeedback> PharmacistFeedbacks { get; set; }
        public DbSet<PharmacyFeedback> PharmacyFeedbacks { get; set; }
    }
}