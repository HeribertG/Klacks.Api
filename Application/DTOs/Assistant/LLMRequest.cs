// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    public string? ModelId { get; set; }

    public string? Language { get; set; }

    public object? Context { get; set; }

    public AssistantPageContext? PageContext { get; set; }
}