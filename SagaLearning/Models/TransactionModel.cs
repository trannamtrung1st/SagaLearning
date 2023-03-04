namespace SagaLearning.Models
{
    public class TransactionModel
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string AccountName { get; set; }
        public double Amount { get; set; }
        public DateTime CreationTime { get; set; }
        public string Description { get; set; }
    }
}