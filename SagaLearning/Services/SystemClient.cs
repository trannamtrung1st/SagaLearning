using SagaLearning.Models;
using System.Net.Http.Json;

namespace SagaLearning.Services
{
    public interface ISystemClient
    {
        Task PushLog(string subject, IEnumerable<string> messages);
    }

    public class SystemClient : ISystemClient
    {
        private readonly HttpClient _httpClient;

        public SystemClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task PushLog(string subject, IEnumerable<string> messages)
        {
            var result = await _httpClient.PostAsJsonAsync("/log", messages.Select(m => new LogModel
            {
                Message = m,
                Subject = subject
            }).ToArray());

            result.EnsureSuccessStatusCode();
        }
    }
}
