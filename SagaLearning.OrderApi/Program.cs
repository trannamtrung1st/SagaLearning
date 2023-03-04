using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SagaLearning.Events;
using SagaLearning.Extensions;
using SagaLearning.Models;
using SagaLearning.OrderApi.Persistence;
using SagaLearning.OrderApi.Services;
using SagaLearning.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseInMemoryDatabase(nameof(OrderDbContext)));

builder.Services.AddKafkaService()
    .AddScoped<IOrderService, OrderService>()
    .AddSystemClient(configuration["SystemApiBaseUrl"]);

var app = builder.Build();

Initialize(app);

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/api/orders", (
    [FromServices] IOrderService orderService) =>
{
    var orders = orderService.GetOrders();
    return Results.Ok(orders);
})
.WithName("Get all orders");

app.MapPost("/api/orders", async (
    [FromBody] SubmitOrderModel model,
    [FromServices] IOrderService orderService,
    [FromServices] IKafkaService kafkaService,
    [FromServices] ISystemClient systemClient) =>
{
    // [NOTE] for demo only, in PROD we should ensure local transaction can be rolled back
    await systemClient.PushLog("OrderApi", new[] { $"[TRANS - Create order]" });

    var order = orderService.CreateOrder(model);

    await systemClient.PushLog("OrderApi", new[]
    {
        $"[TRANS - Create order] {order.Id} | status: {order.Status}",
        $"[EVENT - New order]"
    });

    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(NewOrderEvent), new Confluent.Kafka.Message<string, string>
    {
        Key = order.Id.ToString(),
        Value = JsonSerializer.Serialize(new NewOrderEvent
        {
            OrderId = order.Id,
            Order = order
        })
    });

    return Results.NoContent();
})
.WithName("Submit order");

app.MapGet("/config", () =>
{
    return Results.Ok(Config);
})
.WithName("Get config");

app.MapPut("/config", (
    [FromBody] OrderApiConfigModel model) =>
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

    StartOrderFailureConsumer(kafkaService);

    StartSuccessPaymentConsumer(kafkaService);
}

static void StartOrderFailureConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[]
    {
        nameof(OrderAccountFailureEvent),
        nameof(OrderPaymentFailureEvent),
        nameof(OrderCompletionFailureEvent)
    }, autoCommit: true, async (result, consumer, provider) =>
    {
        var dbContext = provider.GetRequiredService<OrderDbContext>();
        var orderService = provider.GetRequiredService<IOrderService>();
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var message = JsonSerializer.Deserialize<OrderFailureEvent>(result.Message.Value);
        var orderId = message.OrderId;

        await systemClient.PushLog("OrderApi", new[] { $"[TRANS (compensating) - Mark order as failed]" });

        orderService.MarkOrderAsFailed(orderId, message.Reason);

        await systemClient.PushLog("OrderApi", new[] { $"[TRANS (compensating) - Mark order as failed] | {orderId} | reason: {message.Reason}" });
    });
}

static void StartSuccessPaymentConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[]
    {
        nameof(OrderSuccessPaymentEvent)
    }, autoCommit: false, async (result, consumer, provider) =>
    {
        // [NOTE] retryable transaction
        var tryCount = 0;
        var successful = false;
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var message = JsonSerializer.Deserialize<OrderSuccessPaymentEvent>(result.Message.Value);

        while (++tryCount < 5 && !successful)
        {
            try
            {
                var dbContext = provider.GetRequiredService<OrderDbContext>();
                var orderService = provider.GetRequiredService<IOrderService>();
                var orderId = message.OrderId;

                await systemClient.PushLog("OrderApi", new[] { $"[TRANS (retryable) - Mark order as success]" });

                if (Config.CompleteOrderTryCount <= tryCount)
                {
                    orderService.MarkOrderAsSuccess(orderId);

                    successful = true;

                    await systemClient.PushLog("OrderApi", new[] { $"[TRANS (retryable) - Mark order as success] | {orderId} | status: Successful" });
                }
                else
                {
                    throw new Exception($"Failed to complete order {orderId}, try {tryCount}");
                }
            }
            catch (Exception ex)
            {
                await systemClient.PushLog("OrderApi", new[] { $"[EXCEPTION] {ex.Message}" });

                Console.Error.WriteLine(ex);

                Thread.Sleep(1000); // [NOTE] demo
            }
        }

        if (!successful)
        {
            using var producer = kafkaService.CreateProducer<string, string>();

            await systemClient.PushLog("OrderApi", new[] { $"[EVENT - Order completion failure]" });

            await producer.ProduceAsync(nameof(OrderCompletionFailureEvent), new Confluent.Kafka.Message<string, string>
            {
                Key = message.OrderId.ToString(),
                Value = JsonSerializer.Serialize(new OrderCompletionFailureEvent
                {
                    OrderId = message.OrderId,
                    Reason = "Complete order failed"
                })
            });
        }

        consumer.Commit(result);
    });
}

static partial class Program
{
    public static OrderApiConfigModel Config { get; set; } = new OrderApiConfigModel();
}