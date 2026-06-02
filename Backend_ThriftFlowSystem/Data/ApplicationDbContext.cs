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

    }
}
