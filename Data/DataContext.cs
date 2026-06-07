using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        
        public DbSet<Pharmacist> Pharmacists => Set<Pharmacist>();
        public DbSet<JobPost> JobPosts => Set<JobPost>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
        public DbSet<PharmacistFeedback> PharmacistFeedbacks => Set<PharmacistFeedback>();
        public DbSet<PharmacyFeedback> PharmacyFeedbacks => Set<PharmacyFeedback>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobPost>()
                .HasOne(j => j.Pharmacy)
                .WithMany(p => p.JobPosts)
                .HasForeignKey(j => j.PharmacyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.JobPost)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(a => a.JobPostId);
        }
    }
}