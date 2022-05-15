using System.Text.Json;
using Brighter_AzureServiceBus.Ports.Events;
using Paramore.Brighter;

namespace Brighter_AzureServiceBus.Ports.Mappers;

public class TestEventMessageMapper : IAmAMessageMapper<TestEvent>
{
    public Message MapToMessage(TestEvent request)
    {
        var header = new MessageHeader(
            messageId: request.Id,
            topic: "unstable-bulk-gifting-requested-topic",
            messageType: MessageType.MT_EVENT);

        var body = new MessageBody(JsonSerializer.Serialize(request, JsonSerialisationOptions.Options));
        var message = new Message(header, body);

        return message;
    }

    public TestEvent MapToRequest(Message message)
    {
        var testCommand = JsonSerializer.Deserialize<TestEvent>(message.Body.Value, JsonSerialisationOptions.Options);

        return testCommand!;
    }
}