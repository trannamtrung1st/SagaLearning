namespace SagaLearning.WebApp.Models
{
    public class SystemConfigModel
    {
        public bool ShouldTransactionFail { get; set; }
        public bool ShouldInternalPaymentFail { get; set; }
        public bool ShouldExternalPaymentFail { get; set; }
        public int CompleteOrderTryCount { get; set; }
    }
}
