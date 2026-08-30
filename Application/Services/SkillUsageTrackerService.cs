// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class SkillUsageTrackerService : ISkillUsageTracker
{
    private readonly ISkillUsageRepository _repository;
    private readonly ISkillSequenceProactiveNotifier _proactiveNotifier;
    private readonly ILogger<SkillUsageTrackerService> _logger;

    public SkillUsageTrackerService(
        ISkillUsageRepository repository,
        ISkillSequenceProactiveNotifier proactiveNotifier,
        ILogger<SkillUsageTrackerService> logger)
    {
        _repository = repository;
        _proactiveNotifier = proactiveNotifier;
        _logger = logger;
    }

    public async Task TrackAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        SkillResult result,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var record = new SkillUsageRecord
        {
            Id = Guid.NewGuid(),
            SkillName = descriptor.Name,
            Category = descriptor.Category,
            UserId = context.UserId,
            TenantId = context.TenantId,
            ProviderId = context.ProviderId,
            ModelId = context.ModelId,
            SessionId = context.SessionId,
            TurnId = context.TurnId,
            ParametersJson = JsonSerializer.Serialize(SkillParameterRedactor.Redact(parameters)),
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
            _logger.LogError(ex, "Failed to track skill usage for {SkillName}", descriptor.Name);
        }

        if (result.Success)
        {
            try
            {
                await _proactiveNotifier.NotifyAfterSkillAsync(descriptor.Name, context.UserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Proactive skill-sequence suggestion failed for {SkillName}", descriptor.Name);
            }
        }
    }

    public async Task TrackFailureAsync(
        string skillName,
        SkillFailureKind failureKind,
        SkillExecutionContext context,
        Dictionary<string, object>? parameters,
        string? errorMessage,
        TimeSpan duration,
        SkillCategory category = SkillCategory.Action,
        CancellationToken cancellationToken = default)
    {
        var record = new SkillUsageRecord
        {
            Id = Guid.NewGuid(),
            SkillName = skillName,
            Category = category,
            UserId = context.UserId,
            TenantId = context.TenantId,
            ProviderId = context.ProviderId,
            ModelId = context.ModelId,
            SessionId = context.SessionId,
            TurnId = context.TurnId,
            ParametersJson = parameters == null ? null : JsonSerializer.Serialize(SkillParameterRedactor.Redact(parameters)),
            Success = false,
            ErrorMessage = errorMessage,
            FailureKind = failureKind,
            DurationMs = (int)duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await _repository.AddAsync(record, cancellationToken);

            _logger.LogDebug(
                "Skill failure tracked: {SkillName}, Kind: {FailureKind}, User: {UserId}",
                record.SkillName, record.FailureKind, record.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track skill failure for {SkillName} ({FailureKind})", skillName, failureKind);
        }
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetUsageAsync(
        DateTime from,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetRecordsAsync(from, cancellationToken);
    }
}
