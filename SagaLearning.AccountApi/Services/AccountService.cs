using SagaLearning.AccountApi.Entities;
using SagaLearning.AccountApi.Persistence;
using SagaLearning.Models;

namespace SagaLearning.AccountApi.Services
{
    public interface IAccountService
    {
        TransactionResult MakeDecreaseTransaction(Guid orderId, double amount, string description);
        TransactionResult ReverseTransaction(Guid orderId, string description);
    }

    public class AccountService : IAccountService
    {
        private readonly AccountDbContext _dbContext;

        public AccountService(AccountDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public TransactionResult MakeDecreaseTransaction(Guid orderId, double amount, string description)
        {
            if (amount <= 0) throw new ArgumentException(nameof(amount));

            var account = _dbContext.Account.Where(a => a.Name == AccountEntity.DefaultAccountName)
                .FirstOrDefault();

            if (account == null) throw new KeyNotFoundException();

            if (account.Balance - amount < 0) throw new Exception("Not enough balance");

            account.Balance -= amount;

            var transaction = new TransactionEntity
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                AccountName = account.Name,
                Amount = -amount,
                CreationTime = DateTime.Now,
                Description = description
            };

            _dbContext.Transaction.Add(transaction);

            Thread.Sleep(2000); // [NOTE] demo

            _dbContext.SaveChanges();

            var model = new TransactionModel
            {
                Id = transaction.Id,
                OrderId = orderId,
                Amount = transaction.Amount,
                AccountName = account.Name,
                CreationTime = transaction.CreationTime,
                Description = description
            };

            return new TransactionResult
            {
                AccountBalance = account.Balance,
                Transaction = model
            };
        }

        public TransactionResult ReverseTransaction(Guid orderId, string description)
        {
            var account = _dbContext.Account.Where(a => a.Name == AccountEntity.DefaultAccountName)
                .FirstOrDefault();

            if (account == null) throw new KeyNotFoundException();

            var originalTransaction = _dbContext.Transaction
                .Where(t => t.OrderId == orderId)
                .FirstOrDefault();

            account.Balance += -originalTransaction.Amount;

            var reverseTransaction = new TransactionEntity
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                AccountName = originalTransaction.AccountName,
                Amount = -originalTransaction.Amount,
                CreationTime = DateTime.Now,
                Description = description
            };

            _dbContext.Transaction.Add(reverseTransaction);

            Thread.Sleep(2000); // [NOTE] demo

            _dbContext.SaveChanges();

            var model = new TransactionModel
            {
                Id = reverseTransaction.Id,
                OrderId = orderId,
                Amount = reverseTransaction.Amount,
                AccountName = account.Name,
                CreationTime = reverseTransaction.CreationTime,
                Description = description
            };

            return new TransactionResult
            {
                AccountBalance = account.Balance,
                Transaction = model
            };
        }
    }
}
