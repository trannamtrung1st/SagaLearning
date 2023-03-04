using Microsoft.Extensions.DependencyInjection;
using SagaLearning.Services;

namespace SagaLearning.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrderClient(this IServiceCollection services,
            string baseAddress)
        {
            services.AddSingleton<OrderClient>()
                .AddSingleton<IOrderClient>(provider => provider.GetRequiredService<OrderClient>())
                .AddHttpClient<OrderClient>(client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                });

            return services;
        }

        public static IServiceCollection AddAccountClient(this IServiceCollection services,
            string baseAddress)
        {
            services.AddSingleton<AccountClient>()
                .AddSingleton<IAccountClient>(provider => provider.GetRequiredService<AccountClient>())
                .AddHttpClient<AccountClient>(client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                });

            return services;
        }

        public static IServiceCollection AddPaymentClient(this IServiceCollection services,
            string baseAddress)
        {
            services.AddSingleton<PaymentClient>()
                .AddSingleton<IPaymentClient>(provider => provider.GetRequiredService<PaymentClient>())
                .AddHttpClient<PaymentClient>(client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                });

            return services;
        }

        public static IServiceCollection AddSystemClient(this IServiceCollection services,
            string baseAddress)
        {
            services.AddSingleton<SystemClient>()
                .AddSingleton<ISystemClient>(provider => provider.GetRequiredService<SystemClient>())
                .AddHttpClient<SystemClient>(client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                });

            return services;
        }

        public static IServiceCollection AddKafkaService(this IServiceCollection services)
            => services.AddSingleton<IKafkaService, KafkaService>();
    }
}
