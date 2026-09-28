// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRegistry
{
    void Register(SkillDescriptor descriptor);
    void Reload(IReadOnlyList<SkillDescriptor> descriptors);
    void Clear();

    IReadOnlyList<SkillDescriptor> GetAllSkills();
    IReadOnlyList<SkillDescriptor> GetSkillsForUser(IReadOnlyList<string> userPermissions);
    SkillDescriptor? GetSkillByName(string name);

    IReadOnlyList<object> ExportAsProviderFormat(
        LLMProviderType provider,
        IReadOnlyList<string> userPermissions);
}
