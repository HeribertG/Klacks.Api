// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <summary>
/// Sets the caller's explanation-depth override to "advanced" without an LLM turn, sent by the onboarding chip
/// "I already know Klacks".
/// </summary>
/// <param name="UserId">Caller's account id, populated by the controller</param>
public class SetAdvancedExplainDepthCommand : IRequest<ExplainDepthOverrideOutcome>
{
    public string UserId { get; set; } = string.Empty;
}
