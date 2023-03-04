using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SagaLearning.Services
{
    public interface IKafkaService
    {
        IProducer<TKey, TValue> CreateProducer<TKey, TValue>();
        IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(string groupId, bool autoCommit);
        IAdminClient CreateAdmin();
        Thread StartConsumer<TKey, TValue>(
            IEnumerable<string> topics, bool autoCommit,
            Func<ConsumeResult<TKey, TValue>, IConsumer<TKey, TValue>, IServiceProvider, Task> handler);
    }

    public class KafkaService : IKafkaService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public KafkaService(
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public IProducer<TKey, TValue> CreateProducer<TKey, TValue>()
        {
            ProducerConfig producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration["KafkaService:Servers"],
                ClientId = _configuration["KafkaService:ClientId"],
                SecurityProtocol = SecurityProtocol.Plaintext,
                AllowAutoCreateTopics = true
            };

            IProducer<TKey, TValue> producer = new ProducerBuilder<TKey, TValue>(producerConfig).Build();

            return producer;
        }

        public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(string groupId, bool autoCommit)
        {
            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = _configuration["KafkaService:Servers"],
                ClientId = _configuration["KafkaService:ClientId"],
                SecurityProtocol = SecurityProtocol.Plaintext,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = autoCommit,
                GroupInstanceId = "Default"
            };

            IConsumer<TKey, TValue> consumer = new ConsumerBuilder<TKey, TValue>(consumerConfig).Build();

            return consumer;
        }

        public IAdminClient CreateAdmin()
        {
            AdminClientConfig adminConfig = new AdminClientConfig
            {
                BootstrapServers = _configuration["KafkaService:Servers"],
                ClientId = _configuration["KafkaService:ClientId"],
                SecurityProtocol = SecurityProtocol.Plaintext
            };

            IAdminClient adminClient = new AdminClientBuilder(adminConfig).Build();

            return adminClient;
        }

        public Thread StartConsumer<TKey, TValue>(
            IEnumerable<string> topics, bool autoCommit,
            Func<ConsumeResult<TKey, TValue>, IConsumer<TKey, TValue>, IServiceProvider, Task> handler)
        {
            var thread = new Thread(async () =>
            {
                try
                {
                    try
                    {
                        using var adminClient = CreateAdmin();

                        var topicMetadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

                        foreach (var topic in topics)
                        {
                            if (!topicMetadata.Topics.Any(t => t.Topic == topic))
                            {
                                var newTopic = new TopicSpecification { Name = topic };
                                await adminClient.CreateTopicsAsync(new[] { newTopic });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }

                    string groupId = $"{_configuration["KafkaService:ClientId"]}_{string.Join('_', topics)}";

                    using var consumer = CreateConsumer<TKey, TValue>(groupId, autoCommit);

                    try
                    {
                        consumer.Subscribe(topics);

                        while (true)
                        {
                            ConsumeResult<TKey, TValue> message = consumer.Consume();

                            using var scope = _serviceProvider.CreateScope();

                            await handler(message, consumer, scope.ServiceProvider);
                        }
                    }
                    finally
                    {
                        consumer.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            });

            thread.Start();

            return thread;
        }
    }
}
