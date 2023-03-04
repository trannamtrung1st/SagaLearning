using Microsoft.AspNetCore.Mvc;
using SagaLearning.Extensions;
using SagaLearning.Models;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services
    .AddOrderClient(configuration["OrderApiBaseUrl"])
    .AddAccountClient(configuration["AccountApiBaseUrl"])
    .AddPaymentClient(configuration["PaymentApiBaseUrl"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapPost("/log", (
    [FromBody] IEnumerable<LogModel> logs) =>
{
    foreach (var log in logs)
    {
        PushLog(log);
    }

    return Results.NoContent();
})
.WithName("Push log");

app.MapRazorPages();

app.Run();

static partial class Program
{
    public static BlockingCollection<LogModel> Logs { get; } = new BlockingCollection<LogModel>();

    public static void PushLog(string subject, string message)
    {
        Logs.Add(new LogModel
        {
            Subject = subject,
            Message = message
        });
    }

    public static void PushLog(LogModel model)
    {
        if (model != null)
        {
            Logs.Add(model);
        }
    }
}