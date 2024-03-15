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
builder.Services.AddHostedService<ServiceBusReceiverHostedService>(); // Register the hosted service

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

await SendMessagesToServiceBusAsync(app.Services.GetRequiredService<IConfiguration>());

app.Run();

async Task SendMessagesToServiceBusAsync(IConfiguration configuration)
{
    string connectionString = configuration["ServiceBus:ConnectionString"];
    string topicName = configuration["ServiceBus:TopicName"];

    await using var client = new ServiceBusClient(connectionString);
    await using var sender = client.CreateSender(topicName);
    using var messageBatch = await sender.CreateMessageBatchAsync();

    for (int i = 1; i <= 3; i++)
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