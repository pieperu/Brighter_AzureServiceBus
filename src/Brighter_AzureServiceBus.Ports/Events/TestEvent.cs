using Paramore.Brighter;

namespace Brighter_AzureServiceBus.Ports.Events;

public class TestEvent : Event
{
    public TestEvent()
        : base(Guid.NewGuid())
    {
    }
}