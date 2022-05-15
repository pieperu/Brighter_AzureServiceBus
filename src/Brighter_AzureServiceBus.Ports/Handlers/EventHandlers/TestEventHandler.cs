using Brighter_AzureServiceBus.Ports.Events;
using Paramore.Brighter;
using Paramore.Brighter.Logging.Attributes;
using Paramore.Brighter.Policies.Attributes;

namespace Brighter_AzureServiceBus.Ports.Handlers.EventHandlers;

public class TestEventHandler : RequestHandlerAsync<TestEvent>
{
    [RequestLoggingAsync(0, HandlerTiming.Before)]
    [UsePolicyAsync(CommandProcessor.RETRYPOLICYASYNC, 1)]
    public override async Task<TestEvent> HandleAsync(TestEvent command, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new InvalidOperationException("I want this to eventually force the message onto the DLQ");

        return await base.HandleAsync(command, cancellationToken);
    }
}