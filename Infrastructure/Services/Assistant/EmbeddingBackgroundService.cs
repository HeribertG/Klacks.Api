using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class EmbeddingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingBackgroundService> _logger;

    private const int IntervalSeconds = 60;
    private const int BatchSize = 50;

    public EmbeddingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmbeddingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Embedding background service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingEmbeddingsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in embedding background service");
                }

                await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Embedding background service stopped");
    }

    private async Task ProcessPendingEmbeddingsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var memoryRepository = scope.ServiceProvider.GetRequiredService<IAgentMemoryRepository>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        if (!embeddingService.IsAvailable)
            return;

        var pendingMemories = await memoryRepository.GetPendingEmbeddingsAsync(BatchSize, stoppingToken);

        if (pendingMemories.Count == 0)
            return;

        _logger.LogInformation("Generating embeddings for {Count} memories", pendingMemories.Count);

        var generated = 0;
        foreach (var memory in pendingMemories)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var embedding = await embeddingService.GenerateEmbeddingAsync(
                    $"{memory.Key}: {memory.Content}", stoppingToken);

                if (embedding != null)
                {
                    memory.Embedding = embedding;
                    await memoryRepository.UpdateAsync(memory, stoppingToken);
                    generated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for memory {MemoryId}", memory.Id);
            }
        }

        if (generated > 0)
        {
            _logger.LogInformation("Generated {Count} embeddings", generated);
        }
    }
}
