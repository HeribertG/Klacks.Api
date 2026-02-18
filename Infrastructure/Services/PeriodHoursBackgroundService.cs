using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Interfaces;
using System.Threading.Channels;

namespace Klacks.Api.Infrastructure.Services;

public class PeriodHoursBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PeriodHoursBackgroundService> _logger;
    private readonly Channel<PeriodHoursFullRecalculationRequest> _channel;

    public PeriodHoursBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PeriodHoursBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateUnbounded<PeriodHoursFullRecalculationRequest>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
    }

    public void QueueFullRecalculation(DateOnly startDate, DateOnly endDate)
    {
        var request = new PeriodHoursFullRecalculationRequest
        {
            StartDate = startDate,
            EndDate = endDate
        };

        if (!_channel.Writer.TryWrite(request))
        {
            _logger.LogWarning(
                "Failed to queue full period hours recalculation for {StartDate} to {EndDate}",
                startDate,
                endDate);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeriodHoursBackgroundService started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var periodHoursService = scope.ServiceProvider.GetRequiredService<IPeriodHoursService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IWorkNotificationService>();

                    _logger.LogInformation(
                        "Processing full recalculation for {StartDate} to {EndDate}",
                        request.StartDate,
                        request.EndDate);

                    await periodHoursService.RecalculateAllClientsAsync(
                        request.StartDate,
                        request.EndDate);

                    await notificationService.NotifyPeriodHoursRecalculated(
                        request.StartDate,
                        request.EndDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing full period hours recalculation request");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown - ignore
        }

        _logger.LogInformation("PeriodHoursBackgroundService stopped");
    }
}

public class PeriodHoursFullRecalculationRequest
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
