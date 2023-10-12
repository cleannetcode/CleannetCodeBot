using Polly;
using Polly.Retry;

namespace CleannetCodeBot.Twitch;

public static class ServiceCollectionExtension
{
    public const string RetryResiliencePipeline = "retry";
    
    public static IServiceCollection AddRetryResiliencePipeline(this IServiceCollection services)
    {
        services.AddResiliencePipeline(RetryResiliencePipeline, pipelineBuilder =>
        {
            pipelineBuilder
                .AddRetry(new RetryStrategyOptions()
                {
                    MaxRetryAttempts = 5,
                    DelayGenerator = args =>
                    {
                        var delay = args.AttemptNumber switch
                        {
                            0 => TimeSpan.Zero,
                            1 => TimeSpan.FromSeconds(5),
                            2 => TimeSpan.FromSeconds(30),
                            3 => TimeSpan.FromMinutes(1),
                            4 => TimeSpan.FromMinutes(5),
                            _ => TimeSpan.FromMinutes(10),
                        };

                        return new ValueTask<TimeSpan?>(delay);
                    }
                });
        });

        return services;
    }
}