// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates an import token for a drop point via CreateErpImportTokenCommand. Without dropPointId the
/// default drop point is used. The secret is returned exactly once here and is never readable again,
/// so the result message says so explicitly.
/// </summary>
/// <param name="name">Label identifying who or what uses this token (required).</param>
/// <param name="dropPointId">Optional UUID of the drop point; defaults to the default one.</param>
/// <param name="expiresInDays">Optional lifetime in days.</param>

using Klacks.Api.Application.Commands.ErpImportTokens;
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_erp_import_token")]
public class CreateErpImportTokenSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateErpImportTokenSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("Parameter 'name' is required.");
        }

        var dropPointId = GetParameter<Guid?>(parameters, "dropPointId");
        if (!dropPointId.HasValue)
        {
            var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);
            if (dropPoint == null)
            {
                return SkillResult.Error("No default drop point is configured, so no dropPointId could be resolved.");
            }

            dropPointId = dropPoint.Id;
        }

        ErpImportTokenCreatedDto created;
        try
        {
            created = await _mediator.Send(
                new CreateErpImportTokenCommand(
                    dropPointId.Value, name, GetParameter<int?>(parameters, "expiresInDays")),
                cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.DropPointId, created.Name, created.TokenPrefix, created.ExpiresAt, created.Token },
            $"Import token '{created.Name}' created, valid until {created.ExpiresAt:yyyy-MM-dd}. " +
            "Pass the secret on now — it cannot be read again afterwards.");
    }
}
