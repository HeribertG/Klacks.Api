using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillDescriptor(
    string Name,
    string Description,
    SkillCategory Category,
    IReadOnlyList<SkillParameter> Parameters,
    IReadOnlyList<string> RequiredPermissions,
    IReadOnlyList<LLMCapability> RequiredCapabilities,
    Type ImplementationType);
