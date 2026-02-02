using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Services.Skills.Implementations;

public class GetUserContextSkill : BaseSkill
{
    public override string Name => "get_user_context";
    public override string Description => "Get information about the current user and their permissions";
    public override SkillCategory Category => SkillCategory.System;

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var userContext = new
        {
            UserId = context.UserId,
            UserName = context.UserName,
            TenantId = context.TenantId,
            Permissions = context.UserPermissions,
            CurrentPage = context.CurrentPage,
            Timezone = context.UserTimezone ?? "Europe/Berlin"
        };

        return Task.FromResult(SkillResult.SuccessResult(userContext, $"User context for {context.UserName}"));
    }
}
