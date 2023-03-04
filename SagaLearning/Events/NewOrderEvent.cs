using SagaLearning.Models;

namespace SagaLearning.Events
{
    public class NewOrderEvent
    {
        public Guid OrderId { get; set; }
        public OrderModel Order { get; set; }
    }
}
