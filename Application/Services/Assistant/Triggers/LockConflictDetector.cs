// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans agent_skill_executions for failed mutating skill calls whose error message
/// indicates a lock_level conflict (typical phrasing: "locked at level", "lock_level",
/// "immutable"). Emits one LockConflictDetectedTriggerEvent per failure. Lock level is
/// best-effort extracted from the error text via regex; defaults to 1 (Confirmed) when
/// no digit is parsable, which is the most common lock for a Disponent. The work id is likewise
/// best-effort extracted; it is resolved to the groups of the Work's Shift in ONE batched lookup for
/// the whole tick, because that group set is what narrows the notification to the planners who may see
/// the schedule concerned. A conflict whose work id is not parsable keeps Guid.Empty, resolves to no
/// group, and therefore reaches Admins only.
/// </summary>
/// <param name="executionRepository">Reads recent skill executions.</param>
/// <param name="groupScopeReader">Batched work-to-groups lookup for audience scoping.</param>
/// <param name="logger">Structured log per tick.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class LockConflictDetector : IAgentTriggerDetector
{
    private const int LookbackHours = 6;
    private const int DefaultLockLevel = 1;

    private static readonly string[] LockIndicators =
    {
        "lock_level", "locked at level", "lock level", "immutable", "is locked"
    };

    private static readonly Regex LockLevelRegex = new(
        @"(?:lock[_\s]?level|level)\s*[=:]?\s*(\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WorkIdRegex = new(
        @"work[_\s]?id\s*[=:]?\s*([0-9a-f\-]{36})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IAgentSkillExecutionRepository _executionRepository;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly ILogger<LockConflictDetector> _logger;

    public LockConflictDetector(
        IAgentSkillExecutionRepository executionRepository,
        IShiftGroupScopeReader groupScopeReader,
        ILogger<LockConflictDetector> logger)
    {
        _executionRepository = executionRepository;
        _groupScopeReader = groupScopeReader;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.LockConflict;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var sinceUtc = DateTime.UtcNow.AddHours(-LookbackHours);
        var failed = await _executionRepository.GetFailedSinceAsync(sinceUtc, cancellationToken);
        if (failed.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var conflicts = new List<(Guid WorkId, DateOnly Workday, int LockLevel)>();
        foreach (var execution in failed)
        {
            if (string.IsNullOrEmpty(execution.ErrorMessage) && string.IsNullOrEmpty(execution.ResultMessage))
            {
                continue;
            }

            if (!ContainsLockIndicator(execution.ErrorMessage) && !ContainsLockIndicator(execution.ResultMessage))
            {
                continue;
            }

            var combined = (execution.ErrorMessage ?? string.Empty) + " " + (execution.ResultMessage ?? string.Empty);
            var lockLevel = TryParseLockLevel(combined) ?? DefaultLockLevel;
            var workId = TryParseWorkId(combined) ?? Guid.Empty;

            conflicts.Add((workId, DateOnly.FromDateTime(execution.CreateTime ?? DateTime.UtcNow), lockLevel));
        }

        var groupsByWork = await _groupScopeReader.GetGroupIdsByWorkIdsAsync(
            conflicts.Select(conflict => conflict.WorkId).Where(workId => workId != Guid.Empty).ToList(),
            cancellationToken);

        var events = conflicts
            .Select(conflict => (IAgentTriggerEvent)new LockConflictDetectedTriggerEvent(
                conflict.WorkId,
                conflict.Workday,
                conflict.LockLevel,
                ShiftGroupScope.For(groupsByWork, conflict.WorkId)))
            .ToList();

        _logger.LogInformation(
            "LockConflict scan: {Failed} failed execution(s) scanned, {Events} lock conflict event(s) emitted",
            failed.Count, events.Count);

        return events;
    }

    private static bool ContainsLockIndicator(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var indicator in LockIndicators)
        {
            if (text.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static int? TryParseLockLevel(string combined)
    {
        var match = LockLevelRegex.Match(combined);
        if (!match.Success) return null;
        return int.TryParse(match.Groups[1].Value, out var level) ? level : null;
    }

    private static Guid? TryParseWorkId(string combined)
    {
        var match = WorkIdRegex.Match(combined);
        if (!match.Success) return null;
        return Guid.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }
}
