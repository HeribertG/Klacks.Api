// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Revokes an import token of a drop point via RevokeErpImportTokenCommand, which reports success as
/// a boolean. Without dropPointId the default drop point is used.
/// </summary>
/// <param name="tokenId">UUID of the import token to revoke (required).</param>
/// <param name="dropPointId">Optional UUID of the drop point; defaults to the default one.</param>

using Klacks.Api.Application.Commands.ErpImportTokens;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("revoke_erp_import_token")]
public class RevokeErpImportTokenSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public RevokeErpImportTokenSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var tokenId = GetRequiredGuid(parameters, "tokenId");

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

        var revoked = await _mediator.Send(
            new RevokeErpImportTokenCommand(tokenId, dropPointId.Value), cancellationToken);

        if (!revoked)
        {
            return SkillResult.Error($"Import token {tokenId} was not found on this drop point.");
        }

        return SkillResult.SuccessResult(
            new { TokenId = tokenId, DropPointId = dropPointId.Value },
            $"Import token {tokenId} revoked — deliveries using it are turned away from now on.");
    }
}
