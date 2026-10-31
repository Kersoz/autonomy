using Autonomy.Application.Interfaces;
using Npgsql;

namespace Autonomy.Api.BackgroundJobs;

public sealed class JobOfferExpirationService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<JobOfferExpirationService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var jobOfferService = scope.ServiceProvider.GetRequiredService<IJobOfferService>();
                var expiredCount = await jobOfferService.ExpirePendingOffersAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    logger.LogInformation("Expired {Count} pending job offers.", expiredCount);
                }
            }
            catch (NpgsqlException exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(
                    exception,
                    "Failed to expire pending job offers. The expiration job will retry next cycle.");
            }
        }
    }
}
