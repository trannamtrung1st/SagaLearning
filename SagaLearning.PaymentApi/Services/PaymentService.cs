using SagaLearning.Models;
using SagaLearning.PaymentApi.Entities;
using SagaLearning.PaymentApi.Persistence;

namespace SagaLearning.PaymentApi.Services
{
    public interface IPaymentService
    {
        PaymentModel MakePayment(Guid orderId, Guid transactionId, double transactionAmount);
        PaymentModel MarkOrderPaymentAsFailed(Guid orderId);
        void RequestPaymentGateway(Guid paymentId, double amount, string description);
        void RequestRefundOrderPaymentGateway(Guid paymentId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly PaymentDbContext _dbContext;

        public PaymentService(PaymentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public PaymentModel MakePayment(Guid orderId, Guid transactionId, double transactionAmount)
        {
            if (transactionId == default || transactionAmount >= 0) throw new ArgumentException();

            var payment = new PaymentEntity
            {
                Id = Guid.NewGuid(),
                Amount = -transactionAmount,
                TransactionId = transactionId,
                OrderId = orderId,
                Status = "Submitted"
            };

            _dbContext.Payment.Add(payment);

            _dbContext.SaveChanges();

            return new PaymentModel
            {
                Id = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                TransactionId = transactionId,
            };
        }

        public PaymentModel MarkOrderPaymentAsFailed(Guid orderId)
        {
            var payment = _dbContext.Payment
                .Where(p => p.OrderId == orderId)
                .FirstOrDefault();

            if (payment == null) throw new KeyNotFoundException();

            payment.Status = "Failed";

            _dbContext.SaveChanges();

            return new PaymentModel
            {
                Id = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                TransactionId = payment.TransactionId,
            };
        }

        public void RequestPaymentGateway(Guid paymentId, double amount, string description)
        {
            if (Program.Config.ShouldExternalPaymentFail)
            {
                throw new Exception($"PaymentApi: Failed to request payment gateway, amount: {amount}, payment id: {paymentId}");
            }

            Console.WriteLine($"PaymentApi: Successfully requested payment gateway, amount: {amount}, payment id: {paymentId}");
        }

        public void RequestRefundOrderPaymentGateway(Guid paymentId)
        {
            var payment = _dbContext.Payment.Find(paymentId);

            Console.WriteLine($"PaymentApi: Successfully requested refund payment gateway, payment id: {payment.Id}");
        }
    }
}
