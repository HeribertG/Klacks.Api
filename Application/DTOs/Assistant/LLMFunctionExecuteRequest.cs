// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMFunctionExecuteRequest
{
    [Required]
    public string FunctionName { get; set; } = string.Empty;

    public Dictionary<string, object>? Parameters { get; set; }

    public Klacks.Api.Domain.Models.Assistant.AssistantPageContext? PageContext { get; set; }

    public string? Language { get; set; }
}
