using SagaLearning.Models;

namespace SagaLearning.Commands
{
    public class CreateDecreaseTransactionCommand
    {
        public Guid OrderId { get; set; }
        public OrderModel Order { get; set; }
    }
}
