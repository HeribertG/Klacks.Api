// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpSkillExposurePolicy
{
    bool IsExposed(SkillDescriptor descriptor);
}
