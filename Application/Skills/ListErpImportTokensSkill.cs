// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the import tokens of a drop point via GetErpImportTokensQuery. Without dropPointId the
/// default drop point is used, so the common case needs no id. Only the prefix of each token is
/// stored, never the secret itself.
/// </summary>
/// <param name="dropPointId">Optional UUID of the drop point; defaults to the default one.</param>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpImportTokens;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_erp_import_tokens")]
public class ListErpImportTokensSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListErpImportTokensSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
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

        var tokens = await _mediator.Send(new GetErpImportTokensQuery(dropPointId.Value), cancellationToken);

        var projected = tokens
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new { t.Id, t.Name, t.TokenPrefix, t.ExpiresAt, t.LastUsedAt })
            .ToList();

        return SkillResult.SuccessResult(
            new { DropPointId = dropPointId.Value, Count = projected.Count, Tokens = projected },
            $"Found {projected.Count} import token(s) for this drop point.");
    }
}
