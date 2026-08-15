// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Points the user at the settings card that issues a personal access token, and deliberately
/// issues none itself. A skill result travels to the external language-model provider as the tool
/// result of the next loop iteration, so a plaintext token placed here would leave the system.
/// Withholding it from the result is only safe because the skill also stops creating the token:
/// the plaintext is never persisted (PersonalAccessToken stores a SHA-256 hash) and no endpoint
/// reveals it later, so a token minted here could never reach the user at all.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_personal_access_token")]
public class CreatePersonalAccessTokenSkill : BaseSkillImplementation
{
    private const string GuidanceMessage =
        "No token was created, and none can be created from this conversation. " +
        "A personal access token is issued in the settings, on the 'Personal access tokens' card: " +
        "the value appears there once, directly after creation, together with a copy button, and " +
        "is never recoverable afterwards. Tell the user to open that card, create the token there " +
        "with a descriptive name, and copy it immediately. Do not invent, guess or promise a token value.";

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SkillResult.SuccessResult(null, GuidanceMessage));
    }
}
