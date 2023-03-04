namespace SagaLearning.AccountApi.Entities
{
    public class AccountEntity
    {
        public const string DefaultAccountName = "Default";

        public string Name { get; set; }
        public double Balance { get; set; }
    }
}