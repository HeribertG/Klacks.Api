// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves client name slots via the same deterministic ClientResolver the production
/// skills use (accent-insensitive exact match first, then fuzzy) and compares the
/// resolved client's visible id number against the goldset expectation.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class ClientSlotEntityResolver : ISlotEntityResolver
{
    private const string ClientEntityType = "client";
    private const string FirstNameParameter = "firstName";
    private const string LastNameParameter = "lastName";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;

    public ClientSlotEntityResolver(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
    }

    public async Task<bool> ResolvesToExpectedEntityAsync(
        ExpectedEntityRef expected,
        IReadOnlyDictionary<string, object> toolParameters,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(expected.Type, ClientEntityType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var firstName = TurnEvalScorer.GetParameterAsString(toolParameters, FirstNameParameter);
        var lastName = TurnEvalScorer.GetParameterAsString(toolParameters, LastNameParameter);

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return false;
        }

        var (client, _) = await ClientResolver.ResolveByNameAsync(
            _clientSearchRepository, _clientRepository, firstName, lastName, cancellationToken);

        return client != null && client.IdNumber == expected.IdNumber;
    }
}
