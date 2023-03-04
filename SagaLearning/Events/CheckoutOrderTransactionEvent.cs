namespace SagaLearning.Events
{
    public class CheckoutOrderTransactionEvent
    {
        public Guid OrderId { get; set; }
        public string Event { get; set; }
        public string EventPayload { get; set; }
    }
}
