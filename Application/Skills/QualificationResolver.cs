// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the qualification edit skills: resolves a qualification master entry by id
/// or by a multilingual name matched fuzzily (staged NameResolution) across all core-language
/// names, absorbing type-label words like "Qualifikation". Ambiguous or unknown names return
/// the real candidates instead of guessing.
/// </summary>

using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

internal static class QualificationResolver
{
    private static readonly string[] LabelWords =
        ["qualifikation", "qualification", "qualifica"];

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

        var resolution = NameResolution.ResolveVariants(
            qualifications,
            q => MultiLanguage.CoreLanguages.Select(language => q.Name.GetValue(language)),
            qualificationName,
            LabelWords);

        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            var names = string.Join(", ", resolution.Candidates.Select(q => $"{DisplayName(q)} ({q.Id})"));
            return (null, $"'{qualificationName}' is ambiguous. Matching qualifications: {names}. Provide qualificationId instead.");
        }

        var available = qualifications.Count > 0
            ? "Available qualifications: " + string.Join(", ", qualifications.Select(DisplayName)) + "."
            : "There are no qualifications yet.";
        return (null, $"No qualification found matching '{qualificationName}'. {available}");
    }

    public static string DisplayName(Qualification qualification)
    {
        return qualification.Name.De
               ?? qualification.Name.En
               ?? qualification.Name.Fr
               ?? qualification.Name.It
               ?? qualification.Id.ToString();
    }

}
