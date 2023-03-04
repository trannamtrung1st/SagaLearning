using SagaLearning.Models;

namespace SagaLearning.Events
{
    public class NewOrderTransactionEvent
    {
        public Guid OrderId { get; set; }
        public TransactionModel Transaction { get; set; }
    }
}
