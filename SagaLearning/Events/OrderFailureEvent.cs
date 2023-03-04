namespace SagaLearning.Events
{
    public class OrderFailureEvent
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; }
    }
}
