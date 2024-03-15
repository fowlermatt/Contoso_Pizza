using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class MessagingService : BackgroundService
{
    private readonly ILogger<MessagingService> _logger;
    private ServiceBusClient _client;
    private ServiceBusProcessor _processor;
    private readonly string _connectionString = "Endpoint=sb://thepizzaservice.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ayEkgMes/h7U8pVbGIeGNShirAURchgvt+ASbFjtEzo=";
    private readonly string _topicName = "pizzaservice";
    private readonly string _subscriptionName = "S1";

    public MessagingService(ILogger<MessagingService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client = new ServiceBusClient(_connectionString);
        _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        // Start processing
        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Service Bus processor started");

        try
        {
            // Wait indefinitely until the stoppingToken is cancelled
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // This exception is thrown when the stoppingToken is cancelled
            _logger.LogInformation("Task was cancelled");
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        _logger.LogInformation($"Received: {body} from subscription: {_subscriptionName}");
        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception.ToString());
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service Bus processor stopping");
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(stoppingToken);
            _logger.LogInformation("Service Bus processor stopped");
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }
}