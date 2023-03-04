namespace SagaLearning.Models
{
    public class PaymentModel
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public double Amount { get; set; }
        public string Status { get; set; }
    }
}
