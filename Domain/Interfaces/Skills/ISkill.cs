using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Interfaces.Skills;

public interface ISkill
{
    string Name { get; }
    string Description { get; }
    SkillCategory Category { get; }
    IReadOnlyList<SkillParameter> Parameters { get; }
    IReadOnlyList<string> RequiredPermissions { get; }
    IReadOnlyList<LLMCapability> RequiredCapabilities { get; }

    Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
