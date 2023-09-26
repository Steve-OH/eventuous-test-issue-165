using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;
using Microsoft.Extensions.Logging;

namespace EventuousTest;

internal class TestHandler : IEventHandler
{
    public string DiagnosticName => nameof(TestHandler);

    public ValueTask<EventHandlingStatus> HandleEvent(IMessageConsumeContext context)
    {
        count++;
        if (count % 100 == 0)
        {
            logger.LogInformation("{message}", $"Handled {count} events");
        }
        return ValueTask.FromResult(EventHandlingStatus.Success);
    }

    public TestHandler(ILogger<TestHandler> logger)
    {
        this.logger = logger;
    }

    private int count;
    private readonly ILogger<TestHandler> logger;
}
