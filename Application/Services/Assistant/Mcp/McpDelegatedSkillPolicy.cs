// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a skill may be run on behalf of a caller LATER or INDIRECTLY - as the target of a recurring task
/// or as a step or verify skill of a plan. An external agent (MCP, access mode set) may only delegate what it could
/// call over MCP directly: the skill must be exposed (IMcpSkillExposurePolicy) and allowed under its access mode
/// (IMcpReadModeToolPolicy). Without this a schedule or a plan launders a hidden tool (list_personal_access_tokens,
/// a UI action) past the MCP gates. A null access mode (chat, REST, background) is not restricted here.
/// </summary>
/// <param name="exposurePolicy">Which skills are MCP tools at all</param>
/// <param name="readModeToolPolicy">Which exposed tools the caller's access mode permits</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Mcp;

public class McpDelegatedSkillPolicy : IMcpDelegatedSkillPolicy
{
    private readonly IMcpSkillExposurePolicy _exposurePolicy;
    private readonly IMcpReadModeToolPolicy _readModeToolPolicy;

    public McpDelegatedSkillPolicy(IMcpSkillExposurePolicy exposurePolicy, IMcpReadModeToolPolicy readModeToolPolicy)
    {
        _exposurePolicy = exposurePolicy;
        _readModeToolPolicy = readModeToolPolicy;
    }

    public bool IsAllowed(SkillDescriptor descriptor, PersonalAccessTokenAccessMode? externalAgentAccessMode)
    {
        if (externalAgentAccessMode is not { } accessMode)
        {
            return true;
        }

        return _exposurePolicy.IsExposed(descriptor) && _readModeToolPolicy.IsAllowed(descriptor, accessMode);
    }
}
