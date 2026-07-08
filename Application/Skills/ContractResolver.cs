// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the contract-targeting skills: resolves a contract from a user-supplied
/// name via the staged NameResolution matcher. A query decorated with a label word (e.g.
/// "Vertrag Teilzeit 0 Std BE" for the contract "Teilzeit 0 Std BE") resolves to the most
/// specific covered contract, lightly damaged input (missing accents, single typos) is matched
/// fuzzily, and when several contracts remain plausible the resolver returns a disambiguation
/// error listing them instead of silently picking one. The not-found error explicitly tells the
/// model not to retry with the same value, so an unusable slot value cannot cause a retry loop.
/// </summary>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class ContractResolver
{
    public static (Contract? Contract, string? Error) Resolve(
        IReadOnlyList<Contract> contracts, string? contractName)
    {
        var active = contracts
            .Where(c => !c.IsDeleted && !string.IsNullOrWhiteSpace(c.Name))
            .ToList();
        var query = (contractName ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(active, c => c.Name, query);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            var names = string.Join(", ", resolution.Candidates.Select(c => $"'{c.Name}'"));
            return (null,
                $"Multiple contracts match '{query}': {names}. Ask the user which exact contract " +
                "they mean, then call this skill again with that exact name — do not guess.");
        }

        var available = active.Count > 0
            ? "Available contracts: " + string.Join(", ", active.Select(c => c.Name)) + "."
            : "There are no contracts yet.";
        return (null,
            $"No contract found matching '{query}'. {available} " +
            "Do not call this skill again with the same value — pick the exact contract name from " +
            "this list or ask the user which one to assign. Offer the user only these real " +
            "contract names — do not invent contracts.");
    }
}
