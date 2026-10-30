// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Services.Authentication;

/// <summary>
/// Result of resolving one system user account, with the single place that words the refusals.
/// </summary>
/// <param name="Status">Outcome kind</param>
/// <param name="User">The resolved account when the status is Resolved</param>
/// <param name="Candidates">The accounts a name matched, filled for NotFoundByName and Ambiguous</param>
/// <param name="Input">The id or name the caller supplied, empty for MissingInput</param>
public sealed record SystemUserResolution(
    SystemUserResolutionStatus Status,
    AppUser? User,
    IReadOnlyList<UserResource> Candidates,
    string Input)
{
    public string ErrorMessage(bool listCandidates) => Status switch
    {
        SystemUserResolutionStatus.MissingInput => "Either userId or userName must be provided.",
        SystemUserResolutionStatus.NotFoundById => $"User with id '{Input}' not found.",
        SystemUserResolutionStatus.NotFoundByName => $"No user found matching '{Input}'.",
        SystemUserResolutionStatus.Ambiguous when listCandidates =>
            $"'{Input}' is ambiguous. Matching users: " +
            $"{string.Join(", ", Candidates.Select(u => $"{u.FirstName} {u.LastName} ({u.Email})"))}. Provide userId instead.",
        SystemUserResolutionStatus.Ambiguous =>
            $"'{Input}' matches {Candidates.Count} accounts. Provide userId instead.",
        SystemUserResolutionStatus.LoadFailed => $"User '{Input}' could not be loaded.",
        _ => string.Empty
    };
}
