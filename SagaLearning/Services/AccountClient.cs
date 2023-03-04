using SagaLearning.Models;
using System.Net.Http.Json;

namespace SagaLearning.Services
{
    public interface IAccountClient
    {
        Task<AccountApiConfigModel> GetConfig();
        Task UpdateConfig(AccountApiConfigModel model);
    }

    public class AccountClient : IAccountClient
    {
        private readonly HttpClient _httpClient;

        public AccountClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AccountApiConfigModel> GetConfig()
        {
            var result = await _httpClient.GetFromJsonAsync<AccountApiConfigModel>("/config");

            return result;
        }

        public async Task UpdateConfig(AccountApiConfigModel model)
        {
            var result = await _httpClient.PutAsJsonAsync("/config", model);

            result.EnsureSuccessStatusCode();
        }
    }
}
