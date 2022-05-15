using Azure.Messaging.ServiceBus;
using Brighter_AzureServiceBus.Ports;
using Brighter_AzureServiceBus.Ports.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.AzureServiceBus;
using Paramore.Brighter.MessagingGateway.AzureServiceBus.ClientProvider;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using Polly.Registry;
using Serilog;
using Serilog.Events;

namespace Brighter_AzureServiceBus.Host;

public class Program
{
    const string ServiceBusConnectionString = "Endpoint=sb://XXXX.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XXXX";

    private static readonly Subscription[] Subscriptions =
    {
        new AzureServiceBusSubscription<TestEvent>(
            new SubscriptionName(TopicCatalogue.BulkGiftingRequestedTopic),
            new ChannelName(ConsumerCatalogue.TestEventConsumer),
            new RoutingKey(TopicCatalogue.BulkGiftingRequestedTopic),
            timeoutInMilliseconds: 120000,
            makeChannels: OnMissingChannel.Create,
            requeueCount: 2, // We rely on the polly retry policy for this
            isAsync: true,
            noOfPerformers: 1),
    };

    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddSerilog(dispose: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var clientProvider = new ServiceBusConnectionStringClientProvider(ServiceBusConnectionString);
                var asbConsumerFactory = new AzureServiceBusConsumerFactory(clientProvider, false);

                services
                    .AddServiceActivator(options =>
                    {
                        options.Subscriptions = Subscriptions;
                        options.ChannelFactory = new AzureServiceBusChannelFactory(asbConsumerFactory);
                        options.UseScoped = false;
                    })
                    .AutoFromAssemblies();

                services.AddHostedService<ServiceActivatorHostedService>();
                services.AddLogging();

                ConfigureCommandProcessor(services);

                var logger = new LoggerConfiguration()
                    .MinimumLevel.Is(LogEventLevel.Debug)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

                Log.Logger = logger;
                services.AddSingleton<Serilog.ILogger>(logger);

                services.AddSingleton(new ServiceBusClient(ServiceBusConnectionString));
            })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureCommandProcessor(IServiceCollection services)
    {
        services.AddBrighter(configure =>
            {
                configure.PolicyRegistry = RegisterPolicies();
            })
            .AutoFromAssemblies();
    }

    private static IPolicyRegistry<string> RegisterPolicies()
    {
        // Register our policies to handle retry etc. https://github.com/App-vNext/Polly/wiki/Retry
        IPolicyRegistry<string> policyRegistry = new PolicyRegistry
        {
            { CommandProcessor.RETRYPOLICYASYNC, PolicyCatalogue.DefaultRetryAsync },
            { CommandProcessor.RETRYPOLICY, PolicyCatalogue.DefaultRetry },
            { CommandProcessor.CIRCUITBREAKERASYNC, PolicyCatalogue.DefaultCircuitBreakerAsync },
            { CommandProcessor.CIRCUITBREAKER, PolicyCatalogue.DefaultCircuitBreaker },
        };
        return policyRegistry;
    }
}
