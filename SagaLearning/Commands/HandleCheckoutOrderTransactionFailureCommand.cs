namespace SagaLearning.Commands
{
    public class HandleCheckoutOrderTransactionFailureCommand
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; }
    }
}
