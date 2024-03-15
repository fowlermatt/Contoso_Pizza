using ContosoPizza.Data;
using ContosoPizza.Services;
using Microsoft.EntityFrameworkCore;
using Azure.Messaging.ServiceBus;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<PizzaService>();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<PizzaContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ContosoPizzaConnection") ?? "Data Source=ContosoPizza.db"));

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

app.MapRazorPages();

// Add the Azure Service Bus message sending logic here
await SendMessagesToServiceBusAsync(app.Services.GetRequiredService<IConfiguration>());

app.Run();

async Task SendMessagesToServiceBusAsync(IConfiguration configuration)
{
    string connectionString = configuration["ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("Service Bus connection string must be configured.");
    string topicName = configuration["ServiceBus:TopicName"] ?? throw new InvalidOperationException("Service Bus topic name must be configured.");

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