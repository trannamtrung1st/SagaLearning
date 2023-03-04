using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SagaLearning.AccountApi.Persistence;
using SagaLearning.AccountApi.Services;
using SagaLearning.Events;
using SagaLearning.Extensions;
using SagaLearning.Models;
using SagaLearning.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AccountDbContext>(opt =>
    opt.UseInMemoryDatabase(nameof(AccountDbContext)));

builder.Services.AddKafkaService()
    .AddScoped<IAccountService, AccountService>()
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
    [FromBody] AccountApiConfigModel model) =>
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
    var dbContext = provider.GetRequiredService<AccountDbContext>();

    InitData(dbContext);

    StartNewOrderConsumer(kafkaService);

    StartOrderFailureConsumer(kafkaService);
}

static void InitData(AccountDbContext dbContext)
{
    if (dbContext.Database.IsInMemory())
    {
        dbContext.Account.AddRange(AccountDbContext.DefaultAccounts);

        dbContext.SaveChanges();
    }
}

static void StartNewOrderConsumer(IKafkaService kafkaService)
{
    kafkaService.StartConsumer<string, string>(new[] { nameof(NewOrderEvent) }, autoCommit: true, async (result, consumer, provider) =>
    {
        // [NOTE] for demo only, in PROD we should ensure local transaction can be rolled back

        var accountService = provider.GetRequiredService<IAccountService>();
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var message = JsonSerializer.Deserialize<NewOrderEvent>(result.Message.Value);
        var order = message.Order;
        using var producer = kafkaService.CreateProducer<string, string>();

        try
        {
            await systemClient.PushLog("AccountApi", new[] { $"[TRANS - Create decrease transaction]" });

            if (Config.ShouldTransactionFail) // [NOTE] demo
            {
                throw new Exception("AccountApi: Failed to handle new order");
            }

            var transactionResult = accountService.MakeDecreaseTransaction(order.Id, order.Amount, "Checkout order");
            var transaction = transactionResult.Transaction;

            await systemClient.PushLog("AccountApi", new[]
            {
                $"[TRANS - Create decrease transaction] | Account {transaction.AccountName} | transaction id: {transaction.Id} | amount: {transaction.Amount} | balance: {transactionResult.AccountBalance}",
                $"[EVENT - New transaction]"
            });

            await producer.ProduceAsync(nameof(NewOrderTransactionEvent), new Confluent.Kafka.Message<string, string>
            {
                Key = order.Id.ToString(),
                Value = JsonSerializer.Serialize(new NewOrderTransactionEvent
                {
                    OrderId = order.Id,
                    Transaction = transaction
                })
            });
        }
        catch (Exception ex)
        {
            await systemClient.PushLog("AccountApi", new[]
            {
                $"[EXCEPTION] {ex.Message}",
                $"[EVENT - Order account failure]"
            });

            await producer.ProduceAsync(nameof(OrderAccountFailureEvent), new Confluent.Kafka.Message<string, string>
            {
                Key = order.Id.ToString(),
                Value = JsonSerializer.Serialize(new OrderAccountFailureEvent
                {
                    OrderId = order.Id,
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
        nameof(OrderPaymentFailureEvent),
        nameof(OrderCompletionFailureEvent)
    }, autoCommit: true, async (result, consumer, provider) =>
    {
        var dbContext = provider.GetRequiredService<AccountDbContext>();
        var accountService = provider.GetRequiredService<IAccountService>();
        var systemClient = provider.GetRequiredService<ISystemClient>();
        var message = JsonSerializer.Deserialize<OrderFailureEvent>(result.Message.Value);
        var orderId = message.OrderId;
        var description = $"Reverse transaction: {message.Reason}";

        await systemClient.PushLog("AccountApi", new[] { $"[TRANS (compensating) - Reverse transaction]" });

        var transactionResult = accountService.ReverseTransaction(message.OrderId, description);
        var transaction = transactionResult.Transaction;

        await systemClient.PushLog("AccountApi", new[]
        {
            $"[TRANS (compensating) - Reverse transaction] | Account {transaction.AccountName} | transaction id: {transaction.Id} | amount: {transaction.Amount} | balance: {transactionResult.AccountBalance}"
        });
    });
}

static partial class Program
{
    public static AccountApiConfigModel Config { get; set; } = new AccountApiConfigModel();
}