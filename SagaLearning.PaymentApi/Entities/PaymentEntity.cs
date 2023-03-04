namespace SagaLearning.PaymentApi.Entities
{
    public class PaymentEntity
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid TransactionId { get; set; }
        public double Amount { get; set; }
        public string Status { get; set; }
    }
}