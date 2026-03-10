// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Reflection;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("get_system_info")]
public class GetSystemInfoSkill : BaseSkillImplementation
{
    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var systemInfo = new
        {
            ApplicationName = "Klacks",
            Version = version,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            ServerTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            CurrentUser = context.UserName,
            TenantId = context.TenantId
        };

        return Task.FromResult(SkillResult.SuccessResult(systemInfo, "System information retrieved"));
    }
}
