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

        //Authentication related tables
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeInvitation> EmployeeInvitations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<AuthLog> AuthLogs { get; set; }

        // Inventory'n Product related tables
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductLot> ProductLots { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }
        public DbSet<SystemActionLog> SystemActionLogs { get; set; }

        //Pos Order related tables
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ErrorMessage>()
                .ToTable("ErrorMessages");

            // Authentication related configurations
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Username).IsUnique();
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email).IsUnique();

            // Inventory and Product related configurations
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU).IsUnique();
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductLot)
                .WithMany(l => l.Products)
                .HasForeignKey(p => p.ProductLotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order and OrderItem configurations
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Employee)
                .WithMany()
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
