// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;

    [StringLength(GracefulCorrectionDefaults.ConversationIdMaxLength)]
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