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

    /// <summary>
    /// True when the message was sent from the hands-free voice conversation mode.
    /// Suppresses text-only affordances like the [SUGGESTIONS: ...] block.
    /// </summary>
    public bool IsVoiceMode { get; set; }
}