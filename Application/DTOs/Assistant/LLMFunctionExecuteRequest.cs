// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMFunctionExecuteRequest
{
    [Required]
    public string FunctionName { get; set; } = string.Empty;

    public Dictionary<string, object>? Parameters { get; set; }
}
