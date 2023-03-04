using SagaLearning.Models;
using System.Net.Http.Json;

namespace SagaLearning.Services
{
    public interface IPaymentClient
    {
        Task<PaymentApiConfigModel> GetConfig();
        Task UpdateConfig(PaymentApiConfigModel model);
    }

    public class PaymentClient : IPaymentClient
    {
        private readonly HttpClient _httpClient;

        public PaymentClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PaymentApiConfigModel> GetConfig()
        {
            var result = await _httpClient.GetFromJsonAsync<PaymentApiConfigModel>("/config");

            return result;
        }

        public async Task UpdateConfig(PaymentApiConfigModel model)
        {
            var result = await _httpClient.PutAsJsonAsync("/config", model);

            result.EnsureSuccessStatusCode();
        }
    }
}
