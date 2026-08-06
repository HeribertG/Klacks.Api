// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal, single-purpose skill: adds a phone number to an existing client identified by name.
/// Exists to keep the tool schema tiny so the LLM reliably emits the call (the large multi-field
/// update_client is not reliably invoked by the models for edit requests).
/// The write goes to POST api/backend/Communications, the endpoint that owns exactly this one row,
/// rather than reading the whole client and writing it back — that would open a lost-update window
/// across the round trip and run the full client-update machinery for a single insert. The response
/// of the POST is the confirmation that it persisted.
/// </summary>
/// <param name="firstName">First name of the client to update.</param>
/// <param name="lastName">Last name of the client to update.</param>
/// <param name="phone">Phone number to add.</param>

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
public class AddClientPhoneSkill : BaseSkillImplementation
{
    private const string SkillName = "add_client_phone";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public AddClientPhoneSkill(
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
        var phone = GetRequiredString(parameters, "phone");

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var resource = new CommunicationResource
        {
            ClientId = client!.Id,
            Type = CommunicationTypeEnum.PrivateCellPhone,
            Value = phone
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
                $"Adding the phone {phone} for client '{client.FirstName} {client.Name}' returned no result — " +
                "the operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                ClientId = client.Id,
                client.FirstName,
                LastName = client.Name,
                Phone = phone,
                CommunicationId = result.Value.Id
            },
            $"Phone {phone} added to {client.FirstName} {client.Name}.");
    }
}
