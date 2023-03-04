namespace SagaLearning.Models
{
    public class OrderModel
    {
        public Guid Id { get; set; }
        public double Amount { get; set; }
        public string Status { get; set; }
        public string FailureDetails { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
