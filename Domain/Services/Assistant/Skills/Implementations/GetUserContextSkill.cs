// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that returns basic identity information about the current user (name, ID, tenant, current page).
/// Use get_user_permissions instead if you need to check what actions the user is allowed to perform.
/// </summary>
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("get_user_context")]
public class GetUserContextSkill : BaseSkillImplementation
{
    private readonly IEffectiveTimeZoneResolver _timeZoneResolver;

    public GetUserContextSkill(IEffectiveTimeZoneResolver timeZoneResolver)
    {
        _timeZoneResolver = timeZoneResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (_, timezone) = await _timeZoneResolver.ResolveAsync(context.UserTimezone, cancellationToken);

        var userContext = new
        {
            UserId = context.UserId,
            UserName = context.UserName,
            TenantId = context.TenantId,
            Permissions = context.UserPermissions,
            CurrentPage = context.CurrentPage,
            Timezone = timezone
        };

        return SkillResult.SuccessResult(userContext, $"User context for {context.UserName}");
    }
}
