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
        public DbSet<Refund> Refunds { get; set; }

        public DbSet<StoreProfile> StoreProfiles { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<POSShift> POSShifts { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Error Messages

            modelBuilder.Entity<ErrorMessage>()
                .ToTable("ErrorMessages");


            // Authentication
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Username)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            // Inventory & Product
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

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


            // POS & Orders
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

            modelBuilder.Entity<Refund>(entity =>
            {
                entity.HasOne(r => r.Order)
                    .WithMany(o => o.Refunds)
                    .HasForeignKey(r => r.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed Data
            modelBuilder.Entity<StoreProfile>()
                .HasData(new StoreProfile
                    {
                        Id = 1,
                        StoreName = "THRIFT FLOW",
                        Address = "Thailand",
                        ReceiptFooter = "Thank you for shopping!"
                    });

            modelBuilder.Entity<Branch>()
                .HasData(
                    new Branch
                    {
                        Id = 1,
                        BranchName = "Main Store",
                        IsActive = true
                    });
        }
    }
}
