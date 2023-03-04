namespace SagaLearning.Models
{
    public class PaymentApiConfigModel
    {
        public bool ShouldInternalPaymentFail { get; set; }
        public bool ShouldExternalPaymentFail { get; set; }
    }
}
