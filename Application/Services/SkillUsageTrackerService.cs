using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class SkillUsageTrackerService : ISkillUsageTracker
{
    private readonly ISkillUsageRepository _repository;
    private readonly ILogger<SkillUsageTrackerService> _logger;

    public SkillUsageTrackerService(
        ISkillUsageRepository repository,
        ILogger<SkillUsageTrackerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task TrackAsync(
        ISkill skill,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        SkillResult result,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var record = new SkillUsageRecord
        {
            Id = Guid.NewGuid(),
            SkillName = skill.Name,
            Category = skill.Category,
            UserId = context.UserId,
            TenantId = context.TenantId,
            ProviderId = context.ProviderId,
            ModelId = context.ModelId,
            ParametersJson = JsonSerializer.Serialize(parameters),
            Success = result.Success,
            ErrorMessage = result.Success ? null : result.Message,
            DurationMs = (int)duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await _repository.AddAsync(record, cancellationToken);

            _logger.LogDebug(
                "Skill usage tracked: {SkillName}, User: {UserId}, Success: {Success}, Duration: {Duration}ms",
                record.SkillName, record.UserId, record.Success, record.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track skill usage for {SkillName}", skill.Name);
        }
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetUsageAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetRecordsAsync(from, cancellationToken);
    }
}
