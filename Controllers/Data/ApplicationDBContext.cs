using FPSample.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FPSample.Controllers.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<History> Histories { get; set; }
        public DbSet<ServicePurpose> ServicePurposes { get; set; }
        public DbSet<Service> Services { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Explicitly map the Service entity to the "Services" table
            modelBuilder.Entity<Service>().ToTable("Services");

            // 2. Define the Relationship: One Service has many ServiceRequests
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Service)       // ServiceRequest has one Service
                .WithMany()                      // Service can have many requests
                .HasForeignKey(sr => sr.ServiceId) // The link is ServiceId
                .OnDelete(DeleteBehavior.Restrict);

            // 3. (Optional) If your History table links to Admin
            modelBuilder.Entity<History>()
                .HasOne(h => h.Admin)
                .WithMany()
                .HasForeignKey(h => h.AdminId);
        }
    }
}
