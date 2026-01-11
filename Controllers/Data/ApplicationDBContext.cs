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


    }
}
