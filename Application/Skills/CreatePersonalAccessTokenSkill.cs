// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a personal access token (PAT) for the requesting user so that external LLM clients
/// (e.g. Claude Desktop via MCP) can authenticate against the Klacks API.
/// The plaintext token is returned exactly once and must be saved by the user immediately.
/// </summary>
/// <param name="name">Required. A descriptive label for the token (e.g. "Claude Desktop").</param>
/// <param name="expiresInDays">Optional. Validity in days (1–730, default 365).</param>

using Klacks.Api.Application.Commands.Authentification;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_personal_access_token")]
public class CreatePersonalAccessTokenSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreatePersonalAccessTokenSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("name is required.");
        }

        var expiresInDays = GetParameter<int?>(parameters, "expiresInDays");

        var created = await _mediator.Send(
            new CreatePersonalAccessTokenCommand(context.UserId.ToString(), name.Trim(), expiresInDays),
            cancellationToken);

        return SkillResult.SuccessResult(
            new
            {
                created.Id,
                created.Name,
                created.Token,
                created.TokenPrefix,
                ExpiresAt = created.ExpiresAt.ToString("yyyy-MM-dd"),
            },
            $"Personal access token '{created.Name}' created successfully.\n\nToken: {created.Token}\n\nValid until: {created.ExpiresAt:yyyy-MM-dd}\n\n⚠️ This token is shown only once. Copy and store it securely now — it cannot be retrieved again.");
    }
}
