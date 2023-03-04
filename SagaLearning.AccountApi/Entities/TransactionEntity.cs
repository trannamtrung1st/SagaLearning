namespace SagaLearning.AccountApi.Entities
{
    public class TransactionEntity
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string AccountName { get; set; }
        public double Amount { get; set; }
        public DateTime CreationTime { get; set; }
        public string Description { get; set; }

        public virtual AccountEntity Account { get; set; }
    }
}