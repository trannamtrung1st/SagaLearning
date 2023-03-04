using Microsoft.EntityFrameworkCore;
using SagaLearning.AccountApi.Entities;

namespace SagaLearning.AccountApi.Persistence
{
    public class AccountDbContext : DbContext
    {
        public static readonly IEnumerable<AccountEntity> DefaultAccounts = new[]
        {
            new AccountEntity
            {
                Name = AccountEntity.DefaultAccountName,
                Balance = 100000
            }
        };

        public AccountDbContext(DbContextOptions options) : base(options)
        {
        }

        public AccountDbContext()
        {
        }

        public virtual DbSet<AccountEntity> Account { get; set; }
        public virtual DbSet<TransactionEntity> Transaction { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(nameof(AccountDbContext));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountEntity>(builder =>
            {
                builder.HasKey(e => e.Name);

                builder.HasData(DefaultAccounts);
            });

            modelBuilder.Entity<TransactionEntity>(builder =>
            {
                builder.HasKey(e => e.Id);

                builder.HasOne(e => e.Account)
                    .WithMany()
                    .HasForeignKey(e => e.AccountName)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
