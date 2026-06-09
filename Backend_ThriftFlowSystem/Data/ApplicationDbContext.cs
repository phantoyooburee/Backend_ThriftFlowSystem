using Backend_ThriftFlowSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend_ThriftFlowSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<ErrorMessage> ErrorMessages { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeInvitation> EmployeeInvitations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<AuthLog> AuthLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Username).IsUnique();
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email).IsUnique();
            modelBuilder.Entity<ErrorMessage>()
                .ToTable("ErrorMessages");

        }
    }
}
