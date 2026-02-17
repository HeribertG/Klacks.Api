using System.Reflection;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRegistry
{
    void Register(ISkill skill);
    void RegisterFromAssembly(Assembly assembly);

    IReadOnlyList<ISkill> GetAllSkills();
    IReadOnlyList<ISkill> GetSkillsForUser(IReadOnlyList<string> userPermissions);
    IReadOnlyList<ISkill> GetSkillsByCategory(SkillCategory category);
    ISkill? GetSkillByName(string name);

    IReadOnlyList<object> ExportAsProviderFormat(
        LLMProviderType provider,
        IReadOnlyList<string> userPermissions);
}
