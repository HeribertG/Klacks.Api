// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists memberships (client affiliation periods). A membership's ValidFrom is the plannability
/// boundary in the schedule: a client can only be planned from that date on. Optionally filters
/// the result by clientId; the filtering happens in the skill because ListQuery has no parameters.
/// </summary>
/// <param name="clientId">Optional. UUID of a client to restrict the list to that client's memberships.</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_client_memberships")]
public class ListClientMembershipsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListClientMembershipsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdStr = GetParameter<string>(parameters, "clientId");
        Guid? clientFilter = null;

        if (!string.IsNullOrWhiteSpace(clientIdStr))
        {
            if (!Guid.TryParse(clientIdStr, out var parsedClientId))
            {
                return SkillResult.Error($"Invalid client ID format: {clientIdStr}");
            }

            clientFilter = parsedClientId;
        }

        var memberships = await _mediator.Send(new ListQuery<MembershipResource>(), cancellationToken);

        var projected = memberships
            .Where(m => clientFilter == null || m.ClientId == clientFilter.Value)
            .OrderBy(m => m.ClientId)
            .ThenBy(m => m.ValidFrom)
            .Select(m => new
            {
                m.Id,
                m.ClientId,
                m.Type,
                m.ValidFrom,
                m.ValidUntil
            })
            .ToList();

        var resultData = new
        {
            Count = projected.Count,
            Memberships = projected,
            ClientId = clientFilter
        };

        var message = $"Found {projected.Count} membership(s)" +
                      (clientFilter != null ? $" for client {clientFilter}" : "") +
                      ". ValidFrom is the plannability boundary in the schedule.";

        return SkillResult.SuccessResult(resultData, message);
    }
}
