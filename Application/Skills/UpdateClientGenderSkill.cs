// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: updates the gender of a client identified by name.
/// The write goes to PUT api/backend/Clients — this changes the client itself, so the client
/// endpoint is the right one, and the same authorisation, validation and logging apply as to an
/// edit from the browser. The client is read directly (reads may) and sent back as a resource.
/// </summary>
/// <param name="firstName">First name of the client to update.</param>
/// <param name="lastName">Last name of the client to update.</param>
/// <param name="gender">Gender value: female, male, legalEntity, or intersexuality.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Staffs;

using Klacks.Api.Application.Mappers;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateClientGenderSkill : BaseSkillImplementation
{
    private const string SkillName = "update_client_gender";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public UpdateClientGenderSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        ClientMapper clientMapper,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _clientMapper = clientMapper;
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
        var genderStr = GetRequiredString(parameters, "gender");

        if (!Enum.TryParse<GenderEnum>(genderStr, true, out var gender))
        {
            return SkillResult.Error($"Unknown gender value '{genderStr}'. Valid values: female, male, legalEntity, intersexuality.");
        }

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        client!.Gender = gender;

        var result = await _selfApi.PutAsync<ClientResource>(
            _routes.Resolve(typeof(ClientResource)), _clientMapper.ToResource(client), context,
            SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, Gender = gender.ToString() },
            $"Gender of {client.FirstName} {client.Name} updated to {gender}");
    }
}
