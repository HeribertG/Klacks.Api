// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRegistry
{
    void Register(SkillDescriptor descriptor);

    IReadOnlyList<SkillDescriptor> GetAllSkills();
    IReadOnlyList<SkillDescriptor> GetSkillsForUser(IReadOnlyList<string> userPermissions);
    SkillDescriptor? GetSkillByName(string name);

    IReadOnlyList<object> ExportAsProviderFormat(
        LLMProviderType provider,
        IReadOnlyList<string> userPermissions);
}
