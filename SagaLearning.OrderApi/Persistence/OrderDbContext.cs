using Microsoft.EntityFrameworkCore;
using SagaLearning.OrderApi.Entities;

namespace SagaLearning.OrderApi.Persistence
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions options) : base(options)
        {
        }

        public OrderDbContext()
        {
        }

        public virtual DbSet<OrderEntity> Order { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(nameof(OrderDbContext));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderEntity>(builder =>
            {
                builder.HasKey(e => e.Id);
            });
        }
    }
}
