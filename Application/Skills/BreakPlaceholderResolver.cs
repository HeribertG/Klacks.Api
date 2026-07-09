// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a single BreakPlaceholder either directly by its id or by a (clientId, date) pair,
/// requiring exactly one match — ambiguity is reported back with the real candidate ids so the
/// model can ask the user instead of guessing.
/// </summary>
/// <param name="placeholderId">Optional id of the placeholder to load directly</param>
/// <param name="clientId">Client the placeholder belongs to (required when no placeholderId is given)</param>
/// <param name="date">A day covered by the placeholder (required when no placeholderId is given)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Skills;

public static class BreakPlaceholderResolver
{
    public static async Task<(BreakPlaceholder? Placeholder, string? Error)> ResolveAsync(
        IBreakPlaceholderRepository repository,
        Guid? placeholderId,
        Guid? clientId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (placeholderId.HasValue)
        {
            var byId = await repository.Get(placeholderId.Value);
            return byId is null
                ? (null, $"Break placeholder {placeholderId} not found.")
                : (byId, null);
        }

        if (!clientId.HasValue || !date.HasValue)
        {
            return (null, "Provide either placeholderId, or clientId together with date, to identify the planned absence.");
        }

        var dayUtc = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var matches = await repository.GetByClientAndRangeAsync(clientId.Value, dayUtc, dayUtc, cancellationToken);

        if (matches.Count == 0)
        {
            return (null, $"No planned absence (placeholder) found for client {clientId} covering {date:yyyy-MM-dd}.");
        }

        if (matches.Count > 1)
        {
            var options = string.Join("; ", matches.Select(m =>
                $"{m.Id} ({m.From:yyyy-MM-dd}..{m.Until:yyyy-MM-dd})"));
            return (null,
                $"Multiple planned absences cover {date:yyyy-MM-dd} for client {clientId}: {options}. " +
                "Call again with the placeholderId of the one meant.");
        }

        var tracked = await repository.Get(matches[0].Id);
        return tracked is null
            ? (null, $"Break placeholder {matches[0].Id} not found.")
            : (tracked, null);
    }
}
