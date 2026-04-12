using Microsoft.Extensions.Options;
using VibeTravels.Worker.Options;
using VibeTravels.Worker.Processing;

namespace VibeTravels.Worker.HostedServices;

public sealed class JobPollingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<GenerationWorkerOptions> _optionsMonitor;
    private readonly ILogger<JobPollingHostedService> _logger;

    public JobPollingHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<GenerationWorkerOptions> optionsMonitor,
        ILogger<JobPollingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Generation job worker started.");

        while (stoppingToken.IsCancellationRequested is false)
        {
            var options = _optionsMonitor.CurrentValue;
            var idleDelaySeconds = Math.Max(1, options.EmptyPollDelaySeconds);
            var pollDelaySeconds = Math.Max(1, options.PollIntervalSeconds);

            if (options.Enabled is false)
            {
                await DelayAsync(pollDelaySeconds, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var pollingService = scope.ServiceProvider.GetRequiredService<GenerationJobPollingService>();

                //Review 
                // Czy ten delay na pewno to jest potrzeby? Skoro wyzej w tym while tez jest delay?
                var processedJobs = await pollingService.RunOnceAsync(stoppingToken);
                var delaySeconds = processedJobs == 0 ? idleDelaySeconds : pollDelaySeconds;
                await DelayAsync(delaySeconds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Generation worker loop failed.");
                await DelayAsync(pollDelaySeconds, stoppingToken);
            }
        }

        _logger.LogInformation("Generation job worker stopped.");
    }

    private static Task DelayAsync(int seconds, CancellationToken cancellationToken)
        => Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
}
