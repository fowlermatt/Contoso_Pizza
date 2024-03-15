using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Messaging.ServiceBus;

public class SendMessageModel : PageModel
{
    private const string ServiceBusConnectionString = "Endpoint=sb://thepizzaservice.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ayEkgMes/h7U8pVbGIeGNShirAURchgvt+ASbFjtEzo=";
    private const string TopicName = "pizzaservice";

    public string ConfirmationMessage { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        await SendMessagesAsync();
        return Page();
    }

    private async Task SendMessagesAsync()
    {
        await using var client = new ServiceBusClient(ServiceBusConnectionString);
        await using var sender = client.CreateSender(TopicName);
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
            ConfirmationMessage = "A batch of 3 messages has been published to the topic.";
        }
        catch (Exception ex)
        {
            ConfirmationMessage = $"An error occurred: {ex.Message}";
        }
    }
}