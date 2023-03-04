using SagaLearning.Models;
using System.Net.Http.Json;

namespace SagaLearning.Services
{
    public interface IOrderClient
    {
        Task<IEnumerable<OrderModel>> GetOrders();
        Task SubmitOrder(SubmitOrderModel model);
        Task<OrderApiConfigModel> GetConfig();
        Task UpdateConfig(OrderApiConfigModel model);
    }

    public class OrderClient : IOrderClient
    {
        private readonly HttpClient _httpClient;

        public OrderClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<OrderModel>> GetOrders()
        {
            var models = await _httpClient.GetFromJsonAsync<IEnumerable<OrderModel>>("/api/orders");

            return models;
        }

        public async Task SubmitOrder(SubmitOrderModel model)
        {
            var result = await _httpClient.PostAsJsonAsync("/api/orders", model);

            result.EnsureSuccessStatusCode();
        }

        public async Task<OrderApiConfigModel> GetConfig()
        {
            var result = await _httpClient.GetFromJsonAsync<OrderApiConfigModel>("/config");

            return result;
        }

        public async Task UpdateConfig(OrderApiConfigModel model)
        {
            var result = await _httpClient.PutAsJsonAsync("/config", model);

            result.EnsureSuccessStatusCode();
        }
    }
}
