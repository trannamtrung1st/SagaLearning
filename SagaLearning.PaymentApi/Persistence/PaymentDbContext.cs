using Microsoft.EntityFrameworkCore;
using SagaLearning.PaymentApi.Entities;

namespace SagaLearning.PaymentApi.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions options) : base(options)
        {
        }

        public PaymentDbContext()
        {
        }

        public virtual DbSet<PaymentEntity> Payment { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(nameof(PaymentDbContext));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentEntity>(builder =>
            {
                builder.HasKey(e => e.Id);
            });
        }
    }
}
