using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SagaLearning.Commands;
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

    await producer.ProduceAsync(nameof(CheckoutOrderTransactionEvent), new Confluent.Kafka.Message<string, string>
    {
        Key = order.Id.ToString(),
        Value = JsonSerializer.Serialize(new CheckoutOrderTransactionEvent
        {
            OrderId = order.Id,
            Event = nameof(NewOrderEvent),
            EventPayload = JsonSerializer.Serialize(new NewOrderEvent
            {
                OrderId = order.Id,
                Order = order
            })
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

    StartMarkOrderAsFailedConsumer(kafkaService);

    StartMarkOrderAsSuccessConsumer(kafkaService);

    StartCheckoutOrderTransactionSagaOrchestrator(kafkaService);
}

#region Sending commands

static async Task MakePayment(
    Guid orderId, CheckoutOrderTransactionEvent message, IServiceProvider provider)
{
    NewOrderTransactionEvent payload = JsonSerializer.Deserialize<NewOrderTransactionEvent>(message.EventPayload);
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(MakePaymentCommand), new Confluent.Kafka.Message<string, string>
    {
        Key = orderId.ToString(),
        Value = JsonSerializer.Serialize(new MakePaymentCommand
        {
            OrderId = orderId,
            Transaction = payload.Transaction
        })
    });
}

static async Task CreateDecreaseTransaction(
    Guid orderId, CheckoutOrderTransactionEvent message, IServiceProvider provider)
{
    NewOrderEvent payload = JsonSerializer.Deserialize<NewOrderEvent>(message.EventPayload);
    var order = payload.Order;
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(CreateDecreaseTransactionCommand), new Confluent.Kafka.Message<string, string>
    {
        Key = orderId.ToString(),
        Value = JsonSerializer.Serialize(new CreateDecreaseTransactionCommand
        {
            OrderId = orderId,
            Order = order
        })
    });
}

static async Task MarkOrderAsFailed(
    Guid orderId, CheckoutOrderTransactionEvent message, IServiceProvider provider)
{
    OrderFailureEvent payload = JsonSerializer.Deserialize<OrderFailureEvent>(message.EventPayload);
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(MarkOrderAsFailedCommand), new Confluent.Kafka.Message<string, string>
    {
        Key = orderId.ToString(),
        Value = JsonSerializer.Serialize(new MarkOrderAsFailedCommand
        {
            OrderId = orderId,
            Reason = payload.Reason,
        })
    });
}

static async Task MarkOrderAsSuccess(
    Guid orderId, CheckoutOrderTransactionEvent message, IServiceProvider provider)
{
    OrderSuccessPaymentEvent payload = JsonSerializer.Deserialize<OrderSuccessPaymentEvent>(message.EventPayload);
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(MarkOrderAsSuccessCommand), new Confluent.Kafka.Message<string, string>
    {
        Key = orderId.ToString(),
        Value = JsonSerializer.Serialize(new MarkOrderAsSuccessCommand
        {
            OrderId = payload.OrderId
        })
    });
}

static async Task ReverseTransaction(
    Guid orderId, CheckoutOrderTransactionEvent message, IServiceProvider provider)
{
    OrderFailureEvent payload = JsonSerializer.Deserialize<OrderFailureEvent>(message.EventPayload);
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(ReverseTransactionCommand), new Confluent.Kafka.Message<string, string>
    {
        Key = orderId.ToString(),
        Value = JsonSerializer.Serialize(new ReverseTransactionCommand
        {
            OrderId = orderId,
            Reason = payload.Reason,
        })
    });
}

static async Task FailPayment(
    Guid orderId, CheckoutOrderTransactionEvent message, IServiceProvider provider)
{
    OrderFailureEvent payload = JsonSerializer.Deserialize<OrderFailureEvent>(message.EventPayload);
    var kafkaService = provider.GetRequiredService<IKafkaService>();
    using var producer = kafkaService.CreateProducer<string, string>();

    await producer.ProduceAsync(nameof(FailPaymentCommand), new Confluent.Kafka.Message<string, string>
    {
        Key = orderId.ToString(),
        Value = JsonSerializer.Serialize(new FailPaymentCommand
        {
            OrderId = orderId,
            Reason = payload.Reason,
        })
    });
}

#endregion

#region Handlers

static void StartMarkOrderAsFailedConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[]
    {
        nameof(MarkOrderAsFailedCommand),
    }, autoCommit: true, async (result, consumer, provider) =>
    {
        var command = JsonSerializer.Deserialize<MarkOrderAsFailedCommand>(result.Message.Value);
        var orderId = command.OrderId;
        var dbContext = provider.GetRequiredService<OrderDbContext>();
        var orderService = provider.GetRequiredService<IOrderService>();
        var systemClient = provider.GetRequiredService<ISystemClient>();

        await systemClient.PushLog("OrderApi", new[] { $"[TRANS (compensating) - Mark order as failed]" });

        orderService.MarkOrderAsFailed(orderId, command.Reason);

        await systemClient.PushLog("OrderApi", new[] { $"[TRANS (compensating) - Mark order as failed] | {orderId} | reason: {command.Reason}" });
    });
}

static void StartMarkOrderAsSuccessConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[]
    {
        nameof(MarkOrderAsSuccessCommand),
    }, autoCommit: false, async (result, consumer, provider) =>
    {
        // [NOTE] retryable transaction
        var command = JsonSerializer.Deserialize<MarkOrderAsSuccessCommand>(result.Message.Value);
        var orderId = command.OrderId;
        var tryCount = 0;
        var successful = false;
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var kafkaService = provider.GetRequiredService<IKafkaService>();

        while (++tryCount < 5 && !successful)
        {
            try
            {
                var dbContext = provider.GetRequiredService<OrderDbContext>();
                var orderService = provider.GetRequiredService<IOrderService>();

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

            await producer.ProduceAsync(nameof(CheckoutOrderTransactionEvent), new Confluent.Kafka.Message<string, string>
            {
                Key = orderId.ToString(),
                Value = JsonSerializer.Serialize(new CheckoutOrderTransactionEvent
                {
                    OrderId = orderId,
                    Event = nameof(OrderCompletionFailureEvent),
                    EventPayload = JsonSerializer.Serialize(new OrderCompletionFailureEvent
                    {
                        OrderId = orderId,
                        Reason = "Complete order failed"
                    })
                })
            });
        }

        consumer.Commit(result);
    });
}

static void StartCheckoutOrderTransactionSagaOrchestrator(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[]
    {
        nameof(CheckoutOrderTransactionEvent),
    }, autoCommit: false, async (result, consumer, provider) =>
    {
        var message = JsonSerializer.Deserialize<CheckoutOrderTransactionEvent>(result.Message.Value);
        var orderId = message.OrderId;

        switch (message.Event)
        {
            case nameof(NewOrderEvent):
                {
                    await CreateDecreaseTransaction(orderId, message, provider);
                    break;
                }
            case nameof(NewOrderTransactionEvent):
                {
                    await MakePayment(orderId, message, provider);
                    break;
                }
            case nameof(OrderSuccessPaymentEvent):
                {
                    await MarkOrderAsSuccess(orderId, message, provider);
                    break;
                }
            case nameof(OrderAccountFailureEvent):
                {
                    await MarkOrderAsFailed(orderId, message, provider);
                    break;
                }
            case nameof(OrderPaymentFailureEvent):
                {
                    await ReverseTransaction(orderId, message, provider);
                    await MarkOrderAsFailed(orderId, message, provider);
                    break;
                }
            case nameof(OrderCompletionFailureEvent):
                {
                    await FailPayment(orderId, message, provider);
                    await ReverseTransaction(orderId, message, provider);
                    await MarkOrderAsFailed(orderId, message, provider);
                    break;
                }
        }
    });
}

#endregion

static partial class Program
{
    public static OrderApiConfigModel Config { get; set; } = new OrderApiConfigModel();
}