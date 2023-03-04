using SagaLearning.Models;

namespace SagaLearning.Commands
{
    public class MakePaymentCommand
    {
        public Guid OrderId { get; set; }
        public TransactionModel Transaction { get; set; }
    }
}
