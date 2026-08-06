// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes a qualification recorded for a client, identified by name against the qualifications the
/// client currently holds. Verifies the removal against the database after saving.
/// </summary>
/// <param name="firstName">Optional. First name of the client.</param>
/// <param name="lastName">Required. Last name of the client.</param>
/// <param name="qualificationName">Required. Name of the qualification to remove, matched against any of its languages.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.Extensions.Logging;

using Klacks.Api.Application.DTOs.Staffs;

using Klacks.Api.Application.Mappers;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class RemoveClientQualificationSkill : BaseSkillImplementation
{
    private const string SkillName = "remove_client_qualification";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ILogger<RemoveClientQualificationSkill> _logger;

    public RemoveClientQualificationSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IQualificationRepository qualificationRepository,
        ClientMapper clientMapper,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ILogger<RemoveClientQualificationSkill> logger)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _qualificationRepository = qualificationRepository;
        _clientMapper = clientMapper;
        _selfApi = selfApi;
        _routes = routes;
        _logger = logger;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var qualificationName = GetRequiredString(parameters, "qualificationName");

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var assignments = client!.Qualifications.ToList();
        if (assignments.Count == 0)
        {
            return SkillResult.Error($"{client.FirstName} {client.Name} has no qualification recorded.");
        }

        var catalog = await _qualificationRepository.GetAllAsync(cancellationToken);
        var catalogById = catalog.ToDictionary(q => q.Id);

        var held = assignments
            .Select(cq => new
            {
                Assignment = cq,
                Qualification = catalogById.GetValueOrDefault(cq.QualificationId)
            })
            .ToList();

        var matches = held
            .Where(x => x.Qualification != null && NameMatches(x.Qualification.Name, qualificationName))
            .ToList();

        if (matches.Count == 0)
        {
            var heldNames = string.Join(", ", held.Select(x => x.Qualification != null
                ? DisplayName(x.Qualification.Name)
                : "(qualification no longer in the catalogue)"));
            return SkillResult.Error(
                $"No qualification matching '{qualificationName}' is recorded for {client.FirstName} {client.Name}. " +
                $"Currently recorded: {heldNames}.");
        }

        if (matches.Count > 1)
        {
            var options = matches
                .Select(x => new
                {
                    x.Assignment.Id,
                    Name = DisplayName(x.Qualification!.Name),
                    x.Assignment.Level,
                    x.Assignment.ValidFrom,
                    x.Assignment.ValidUntil
                })
                .ToList();

            return SkillResult.SuccessResult(
                new { ClientId = client.Id, Candidates = options },
                $"{client.FirstName} {client.Name} has {matches.Count} matching qualifications. Specify which one by name or validity.");
        }

        var target = matches[0].Assignment;
        var qualificationLabel = DisplayName(matches[0].Qualification!.Name);
        var targetId = target.Id;

        client.Qualifications.Remove(target);
        client.UpdateTime = DateTime.UtcNow;
        client.CurrentUserUpdated = context.UserName;

        var result = await _selfApi.PutAsync<ClientResource>(
            _routes.Resolve(typeof(ClientResource)), _clientMapper.ToResource(client), context,
            SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, QualificationName = qualificationLabel },
            $"Qualification '{qualificationLabel}' removed from {client.FirstName} {client.Name}");
    }

    private static bool NameMatches(MultiLanguage name, string term)
    {
        return name.GetAllValues().Values.Any(v => v != null && v.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string DisplayName(MultiLanguage name)
    {
        return name.De ?? name.En ?? name.Fr ?? name.It ?? "(unnamed)";
    }
}
