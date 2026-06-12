// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Presentation.Mcp;

public interface IMcpSkillExposurePolicy
{
    bool IsExposed(SkillDescriptor descriptor);
}
