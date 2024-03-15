using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ContosoPizza.Data;
using ContosoPizza.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<PizzaService>();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<PizzaContext>(options =>
    options.UseSqlite("Data Source=ContosoPizza.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Directly use the connection string and topic name here
const string connectionString = "Endpoint=sb://thepizzaservice.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ayEkgMes/h7U8pVbGIeGNShirAURchgvt+ASbFjtEzo=";
const string topicName = "pizzaservice";

await SendMessagesToServiceBusAsync(connectionString, topicName);

app.Run();

async Task SendMessagesToServiceBusAsync(string connectionString, string topicName)
{
    await using var client = new ServiceBusClient(connectionString);
    await using var sender = client.CreateSender(topicName);
    using var messageBatch = await sender.CreateMessageBatchAsync();

    for (int i = 1; i <= 3; i++) // Example: sending 3 messages
    {
        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}")))
        {
            throw new Exception($"The message {i} is too large to fit in the batch.");
        }
    }

    try
    {
        await sender.SendMessagesAsync(messageBatch);
        Console.WriteLine($"A batch of 3 messages has been published to the topic.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}

Console.WriteLine("Press any key to end the application");
Console.ReadKey();