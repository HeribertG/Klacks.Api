// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    public const int MaxListedContractNames = 20;

    public static (Contract? Contract, string? Error) Resolve(
        IReadOnlyList<Contract> contracts, string? contractName)
    {
        var active = ActiveContracts(contracts);
        var query = (contractName ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(active, c => c.Name, query);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        return (null, resolution.Candidates.Count > 1
            ? AmbiguousMessage(query, resolution.Candidates)
            : NotFoundMessage(query, active));
    }

    public static (Contract? Contract, string? Error) ResolveExactOrUniquePartial(
        IReadOnlyList<Contract> contracts, string? contractName)
    {
        var active = ActiveContracts(contracts);
        var query = (contractName ?? string.Empty).Trim();

        var exact = active.Where(c => string.Equals(c.Name.Trim(), query, StringComparison.OrdinalIgnoreCase)).ToList();
        var matches = exact.Count > 0
            ? exact
            : query.Length == 0
                ? []
                : active.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        return matches.Count switch
        {
            1 => (matches[0], null),
            > 1 => (null, AmbiguousMessage(query, matches)),
            _ => (null, NotFoundMessage(query, active))
        };
    }

    private static List<Contract> ActiveContracts(IReadOnlyList<Contract> contracts) =>
        contracts.Where(c => !c.IsDeleted && !string.IsNullOrWhiteSpace(c.Name)).ToList();

    private static string AmbiguousMessage(string query, IReadOnlyList<Contract> candidates)
    {
        var names = string.Join(", ", candidates.Select(c => $"'{c.Name}'"));
        return $"Multiple contracts match '{query}': {names}. Ask the user which exact contract " +
               "they mean, then call this skill again with that exact name — do not guess.";
    }

    private static string NotFoundMessage(string query, IReadOnlyList<Contract> active)
    {
        var available = active.Count > 0
            ? "Available contracts: " + ListNames(active) + "."
            : "There are no contracts yet.";
        return $"No contract found matching '{query}'. {available} " +
               "Do not call this skill again with the same value — pick the exact contract name from " +
               "this list or ask the user which one is meant. Offer the user only these real " +
               "contract names — do not invent contracts.";
    }

    private static string ListNames(IReadOnlyList<Contract> contracts)
    {
        var shown = string.Join(", ", contracts.Take(MaxListedContractNames).Select(c => c.Name));
        return contracts.Count > MaxListedContractNames
            ? $"{shown} and {contracts.Count - MaxListedContractNames} more"
            : shown;
    }
}
