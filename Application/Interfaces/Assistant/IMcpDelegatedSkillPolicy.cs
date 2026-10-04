// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IMcpDelegatedSkillPolicy
{
    bool IsAllowed(SkillDescriptor descriptor, PersonalAccessTokenAccessMode? externalAgentAccessMode);
}
