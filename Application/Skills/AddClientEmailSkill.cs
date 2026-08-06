// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: adds an email address to an existing client identified by name.
/// Kept tiny so the LLM reliably emits the call (large multi-field update_client is not).
/// The write goes to POST api/backend/Communications, the endpoint that owns exactly this one row.
/// Routing it through the client endpoint instead would mean reading the whole client, changing one
/// collection and writing it all back — which opens a lost-update window across the round trip and
/// drags the full client-update machinery (contract-change events, an inbox re-assignment sweep) along
/// for what is a single insert. The response of the POST is the confirmation that it persisted.
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="email">Email address to add.</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AddClientEmailSkill : BaseSkillImplementation
{
    private const string SkillName = "add_client_email";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public AddClientEmailSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _selfApi = selfApi;
        _routes = routes;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var email = GetRequiredString(parameters, "email");

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var resource = new CommunicationResource
        {
            ClientId = client!.Id,
            Type = CommunicationTypeEnum.PrivateMail,
            Value = email
        };

        var result = await _selfApi.PostAsync<CommunicationResource>(
            _routes.Resolve(typeof(CommunicationResource)), resource, context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        if (result.Value == null)
        {
            return SkillResult.Error(
                $"Adding the email {email} for client '{client.FirstName} {client.Name}' returned no result — " +
                "the operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                ClientId = client.Id,
                client.FirstName,
                LastName = client.Name,
                Email = email,
                CommunicationId = result.Value.Id
            },
            $"Email {email} added to {client.FirstName} {client.Name}.");
    }
}
