// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Points the user at the settings card that issues an import key for a handover point, and
/// deliberately issues none itself. A skill result travels to the external language-model provider
/// as the tool result of the next loop iteration, so a plaintext key placed here would leave the
/// system. Withholding it from the result is only safe because the skill also stops creating the
/// key: the secret is never persisted (ErpImportToken stores a SHA-256 hash) and no endpoint
/// reveals it later, so a key minted here could never reach the user at all.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_erp_import_token")]
public class CreateErpImportTokenSkill : BaseSkillImplementation
{
    private const string GuidanceMessage =
        "No key was issued, and none can be issued from this conversation. " +
        "An import key is created in the settings of the handover point, on the 'Tokens' card of " +
        "the ERP handover points: the secret appears there once, directly after creation, together " +
        "with a copy button, and is never recoverable afterwards. Tell the user to open that card, " +
        "create the key there with a label naming who uses it, and pass it on immediately. " +
        "Do not invent, guess or promise a key value.";

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SkillResult.SuccessResult(null, GuidanceMessage));
    }
}
