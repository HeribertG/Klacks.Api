using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Hubs;
using System.Threading.Channels;

namespace Klacks.Api.Infrastructure.Services;

public class PeriodHoursBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PeriodHoursBackgroundService> _logger;
    private readonly Channel<PeriodHoursRecalculationRequest> _channel;

    public PeriodHoursBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PeriodHoursBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateUnbounded<PeriodHoursRecalculationRequest>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
    }

    public void QueueRecalculation(Guid clientId, DateOnly date)
    {
        var request = new PeriodHoursRecalculationRequest
        {
            ClientId = clientId,
            Date = date
        };

        if (!_channel.Writer.TryWrite(request))
        {
            _logger.LogWarning(
                "Failed to queue period hours recalculation for client {ClientId}",
                clientId);
        }
    }

    public void QueueFullRecalculation(DateOnly startDate, DateOnly endDate)
    {
        var request = new PeriodHoursRecalculationRequest
        {
            IsFullRecalculation = true,
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

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var periodHoursService = scope.ServiceProvider.GetRequiredService<IPeriodHoursService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IPeriodHoursNotificationService>();

                if (request.IsFullRecalculation)
                {
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
                else
                {
                    _logger.LogDebug(
                        "Processing recalculation for client {ClientId} at {Date}",
                        request.ClientId,
                        request.Date);

                    await periodHoursService.InvalidateCacheAsync(request.ClientId, request.Date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing period hours recalculation request");
            }
        }

        _logger.LogInformation("PeriodHoursBackgroundService stopped");
    }
}

public class PeriodHoursRecalculationRequest
{
    public Guid ClientId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsFullRecalculation { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
