using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace Brighter_AzureServiceBus.Ports;

public static class PolicyCatalogue
{
    public static RetryPolicy DefaultRetry { get; set; }
    public static readonly AsyncRetryPolicy DefaultRetryAsync;
    public static CircuitBreakerPolicy DefaultCircuitBreaker { get; set; }
    public static AsyncCircuitBreakerPolicy DefaultCircuitBreakerAsync { get; set; }
    
    static PolicyCatalogue()
    {
        // TODO : Sort out these policies #81790
        var delay = Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromMilliseconds(100),
                retryCount: 2,
                fastFirst: true)
            .ToList();

        var sleepDurations = new[]
        {
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(150),
        };

        DefaultRetryAsync = Policy.Handle<Exception>().WaitAndRetryAsync(delay, (exception, timeTilRetry, retryAttempt, context) =>
        {
            LogRetryException(exception, context, retryAttempt, nameof(DefaultRetryAsync));
        });
        
        DefaultRetry = Policy.Handle<Exception>().WaitAndRetry(sleepDurations);

        DefaultCircuitBreakerAsync = Policy.Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromMilliseconds(150));

        DefaultCircuitBreaker = Policy.Handle<Exception>()
            .CircuitBreaker(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromMilliseconds(150));
    }

    private static void LogRetryException(Exception? exception, Context context, int retryCount, string policyName)
    {
        if (exception != null)
        {
            Console.WriteLine(
                $"Retrying {policyName}:{context.PolicyKey} attempt {retryCount}. Exception : {exception.Message}");
        }
        else
        {
            Console.WriteLine(
                $"Retrying {policyName}:{context.PolicyKey} attempt {retryCount}");
        }
    }
}
