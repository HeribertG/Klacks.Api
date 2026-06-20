// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Hard-deletes a user's personal assistant data on account deletion (GDPR erasure). Uses
/// ExecuteDelete with IgnoreQueryFilters so it bypasses both the soft-delete interceptor and the
/// is-deleted query filter — the rows are physically removed, including any previously soft-deleted
/// ones. Only rows owned by the given user are touched; shared/company data (e.g. AgentMemory with
/// a null UserId) is preserved. Intended to run inside the account-deletion transaction.
/// </summary>

namespace Klacks.Api.Infrastructure.Services;

using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class UserDataEraser : IUserDataEraser
{
    private readonly DataBaseContext _context;
    private readonly ILogger<UserDataEraser> _logger;

    public UserDataEraser(DataBaseContext context, ILogger<UserDataEraser> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var uid = userId.ToString();
        var removed = 0;

        // Personal memories (per-user interests/preferences). UserId is a Guid; company memories
        // (UserId == null) are deliberately preserved. Tags cascade via the AgentMemoryTag FK.
        removed += await _context.AgentMemories.IgnoreQueryFilters()
            .Where(m => m.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        // Per-user skill telemetry (Guid UserId).
        removed += await _context.SkillUsageRecords.IgnoreQueryFilters()
            .Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        // Agent session messages first (children), then the sessions themselves (parents), so the
        // delete is safe regardless of the message->session FK delete behaviour.
        removed += await _context.AgentSessionMessages.IgnoreQueryFilters()
            .Where(m => _context.AgentSessions.Any(s => s.Id == m.SessionId && s.UserId == uid))
            .ExecuteDeleteAsync(cancellationToken);
        removed += await _context.AgentSessions.IgnoreQueryFilters()
            .Where(s => s.UserId == uid).ExecuteDeleteAsync(cancellationToken);

        // Remaining user-scoped assistant rows with a loose string UserId and no FK cascade.
        removed += await _context.AgentTriggerPreferences.IgnoreQueryFilters()
            .Where(p => p.UserId == uid).ExecuteDeleteAsync(cancellationToken);
        removed += await _context.AgentAutonomyPreferences.IgnoreQueryFilters()
            .Where(p => p.UserId == uid).ExecuteDeleteAsync(cancellationToken);
        removed += await _context.AgentPlans.IgnoreQueryFilters()
            .Where(p => p.UserId == uid).ExecuteDeleteAsync(cancellationToken);
        removed += await _context.AgentSkillExecutions.IgnoreQueryFilters()
            .Where(e => e.UserId == uid).ExecuteDeleteAsync(cancellationToken);
        removed += await _context.HeartbeatConfigs.IgnoreQueryFilters()
            .Where(h => h.UserId == uid).ExecuteDeleteAsync(cancellationToken);
        removed += await _context.SkillSelectionTrajectories.IgnoreQueryFilters()
            .Where(t => t.UserId == uid).ExecuteDeleteAsync(cancellationToken);
        removed += await _context.ClientSortPreference.IgnoreQueryFilters()
            .Where(c => c.UserId == uid).ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("GDPR erasure: hard-deleted {Count} assistant data row(s) for user {UserId}", removed, userId);
    }
}
