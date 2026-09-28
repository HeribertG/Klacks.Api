// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillChainExecuteRequest
{
    public required IReadOnlyList<SkillInvocationDto> Invocations { get; init; }
}

public record SkillInvocationDto
{
    public required string SkillName { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public bool StopOnError { get; init; } = true;
}
