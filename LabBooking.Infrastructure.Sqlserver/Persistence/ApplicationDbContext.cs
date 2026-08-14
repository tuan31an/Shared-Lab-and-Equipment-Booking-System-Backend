using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LabBooking.Infrastructure.Sqlserver.Persistence
{
    public class ApplicationDbContext : DbContext, IUnitOfWork
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<PriorityRule> PriorityRules => Set<PriorityRule>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Waitlist> Waitlists => Set<Waitlist>();
        public DbSet<CheckInOut> CheckInOuts => Set<CheckInOut>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<Maintenance> Maintenances => Set<Maintenance>();
        public DbSet<Violation> Violations => Set<Violation>();
        public DbSet<Restriction> Restrictions => Set<Restriction>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
