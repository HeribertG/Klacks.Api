// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the contract reference a skill received into a contract id without ever throwing for a malformed value.
/// A reference that parses as a GUID is taken as the id (the caller loads the contract and reports a missing one).
/// Anything else is treated as a contract name and resolved against the non-deleted contracts with the staged name
/// matcher the other contract skills use; the ContractNameMatchMode decides how far that goes: skills that write
/// accept only an exact or unique partial name (a fuzzy hit could change a contract other than the one the user
/// confirmed), read-only skills may add the typo and phonetic stages. No match or several matches produce an error that lists the real contract names, so the model never has to guess or invent an id. A weak
/// model that only knows the name from the conversation ("Vollzeit 160 BE") is the reason: it passes the name
/// where an id is expected. ParseStrictId is for destructive skills that must stay id-only: it never matches by
/// name, but a malformed value is still a clean error instead of an exception.
/// </summary>
/// <param name="contractRepository">Lists the contracts a name reference is resolved against</param>
/// <param name="reference">The raw parameter value: a contract id or a contract name</param>
/// <param name="parameterName">The skill parameter the value came from, used in the error text</param>
/// <param name="mode">How a name reference is matched: exact or unique partial only, or with the fuzzy stages</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractReferenceResolver
{
    public static async Task<(Guid? ContractId, string? Error)> ResolveIdAsync(
        IContractRepository contractRepository,
        string? reference,
        string parameterName,
        ContractNameMatchMode mode,
        CancellationToken cancellationToken)
    {
        var trimmed = reference?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return (null, MissingMessage(parameterName));
        }

        if (Guid.TryParse(trimmed, out var contractId))
        {
            return (contractId, null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var contracts = await contractRepository.List();
        var (contract, error) = mode == ContractNameMatchMode.WithFuzzy
            ? ContractResolver.Resolve(contracts, trimmed)
            : ContractResolver.ResolveExactOrUniquePartial(contracts, trimmed);

        return contract != null ? (contract.Id, null) : (null, error);
    }

    public static (Guid? ContractId, string? Error) ParseStrictId(string? reference, string parameterName)
    {
        var trimmed = reference?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return (null, MissingMessage(parameterName));
        }

        return Guid.TryParse(trimmed, out var contractId)
            ? (contractId, null)
            : (null, $"Parameter '{parameterName}' must be the contract id (UUID), not '{trimmed}'. " +
                     "Find the id with the contract list first; this skill does not match by name.");
    }

    private static string MissingMessage(string parameterName) =>
        $"Missing required parameter '{parameterName}' (contract id or exact contract name).";
}
