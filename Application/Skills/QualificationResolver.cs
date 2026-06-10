// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the qualification edit skills: resolves a qualification master entry
/// by id or by an exact, unambiguous multilingual name via the qualification list query.
/// </summary>

using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

internal static class QualificationResolver
{
    public static async Task<(Qualification? Qualification, string? Error)> ResolveAsync(
        IMediator mediator,
        string? qualificationId,
        string? qualificationName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(qualificationId))
        {
            if (!Guid.TryParse(qualificationId, out var id))
            {
                return (null, $"'{qualificationId}' is not a valid qualification id.");
            }

            var qualificationsById = await mediator.Send(new ListQuery(), cancellationToken);
            var byId = qualificationsById.FirstOrDefault(q => q.Id == id);
            return byId != null
                ? (byId, null)
                : (null, $"No qualification found with id '{id}'.");
        }

        if (string.IsNullOrWhiteSpace(qualificationName))
        {
            return (null, "Either qualificationId or qualificationName must be provided.");
        }

        var qualifications = (await mediator.Send(new ListQuery(), cancellationToken)).ToList();

        var exact = qualifications.Where(q => MatchesName(q, qualificationName, true)).ToList();
        var candidates = exact.Count > 0
            ? exact
            : qualifications.Where(q => MatchesName(q, qualificationName, false)).ToList();

        if (candidates.Count == 0)
        {
            return (null, $"No qualification found matching '{qualificationName}'.");
        }

        if (candidates.Count > 1)
        {
            var names = string.Join(", ", candidates.Select(q => $"{DisplayName(q)} ({q.Id})"));
            return (null, $"'{qualificationName}' is ambiguous. Matching qualifications: {names}. Provide qualificationId instead.");
        }

        return (candidates[0], null);
    }

    public static string DisplayName(Qualification qualification)
    {
        return qualification.Name.De
               ?? qualification.Name.En
               ?? qualification.Name.Fr
               ?? qualification.Name.It
               ?? qualification.Id.ToString();
    }

    private static bool MatchesName(Qualification qualification, string name, bool exact)
    {
        return MultiLanguage.CoreLanguages
            .Select(language => qualification.Name.GetValue(language))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Any(value => exact
                ? value!.Equals(name, StringComparison.OrdinalIgnoreCase)
                : value!.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}
