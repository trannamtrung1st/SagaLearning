using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SagaLearning.Events;
using SagaLearning.Extensions;
using SagaLearning.Models;
using SagaLearning.PaymentApi.Persistence;
using SagaLearning.PaymentApi.Services;
using SagaLearning.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseInMemoryDatabase(nameof(PaymentDbContext)));

builder.Services.AddKafkaService()
    .AddScoped<IPaymentService, PaymentService>()
    .AddSystemClient(configuration["SystemApiBaseUrl"]);

var app = builder.Build();

Initialize(app);

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/config", () =>
{
    return Results.Ok(Config);
})
.WithName("Get config");

app.MapPut("/config", (
    [FromBody] PaymentApiConfigModel model) =>
{
    Config = model;

    return Results.NoContent();
})
.WithName("Update config");

app.Run();

static void Initialize(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var provider = scope.ServiceProvider;
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    var dbContext = provider.GetRequiredService<PaymentDbContext>();

    StartNewTransactionConsumer(kafkaService);

    StartOrderFailureConsumer(kafkaService);
}

static void StartNewTransactionConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[] { nameof(NewOrderTransactionEvent) }, autoCommit: true, async (result, consumer, provider) =>
    {
        // [NOTE] for demo only, in PROD we should ensure local transaction can be rolled back

        var paymentService = provider.GetRequiredService<IPaymentService>();
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var message = JsonSerializer.Deserialize<NewOrderTransactionEvent>(result.Message.Value);
        var transaction = message.Transaction;
        using var producer = kafkaService.CreateProducer<string, string>();

        try
        {
            await systemClient.PushLog("PaymentApi", new[] { $"[TRANS (pivot) - Create & Make payment via gateway]" });

            if (Config.ShouldInternalPaymentFail) // [NOTE] demo
            {
                throw new Exception("PaymentApi: Failed to handle new transaction");
            }

            var payment = paymentService.MakePayment(transaction.OrderId, transaction.Id, transaction.Amount);

            paymentService.RequestPaymentGateway(payment.Id, payment.Amount, "Request payment");

            await systemClient.PushLog("PaymentApi", new[]
            {
                $"[TRANS (pivot) - Create & Make payment via gateway] payment id: {payment.Id} | status: {payment.Status} | amount: {payment.Amount}",
                $"[EVENT - Success payment]"
            });

            await producer.ProduceAsync(nameof(OrderSuccessPaymentEvent), new Confluent.Kafka.Message<string, string>
            {
                Key = message.OrderId.ToString(),
                Value = JsonSerializer.Serialize(new OrderSuccessPaymentEvent
                {
                    OrderId = message.OrderId
                })
            });
        }
        catch (Exception ex)
        {
            await systemClient.PushLog("PaymentApi", new[]
            {
                $"[EXCEPTION] {ex.Message}",
                $"[EVENT - Order payment failure]"
            });

            await producer.ProduceAsync(nameof(OrderPaymentFailureEvent), new Confluent.Kafka.Message<string, string>
            {
                Key = transaction.Id.ToString(),
                Value = JsonSerializer.Serialize(new OrderPaymentFailureEvent
                {
                    OrderId = message.OrderId,
                    Reason = ex.Message,
                })
            });

            Thread.Sleep(5000);
        }
    });
}

static void StartOrderFailureConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[]
    {
        nameof(OrderCompletionFailureEvent)
    }, autoCommit: true, async (result, consumer, provider) =>
    {
        var paymentService = provider.GetRequiredService<IPaymentService>();
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var message = JsonSerializer.Deserialize<OrderCompletionFailureEvent>(result.Message.Value);
        var orderId = message.OrderId;

        await systemClient.PushLog("PaymentApi", new[] { $"[TRANS (compensating) - Mark payment as failed & request refund payment]" });

        var payment = paymentService.MarkOrderPaymentAsFailed(orderId);

        paymentService.RequestRefundOrderPaymentGateway(payment.Id);

        await systemClient.PushLog("PaymentApi", new[]
        {
            $"[TRANS (compensating) - Mark payment as failed & request refund payment] payment id: {payment.Id} | status: {payment.Status} | amount: {payment.Amount}"
        });
    });
}

static partial class Program
{
    public static PaymentApiConfigModel Config { get; set; } = new PaymentApiConfigModel();
}