// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to process an LLM chat message with intelligent skill filtering.
/// The toolset is built by ISkillToolsetAssembler, shared with the streaming path.
/// </summary>
/// <param name="Message">User's chat message.</param>
/// <param name="UserRights">User's permissions for skill access control.</param>
/// <param name="ModelId">Optional specific LLM model to use.</param>
/// <param name="Language">User's UI language, any of the 25 supported languages; NOT necessarily the language the message is written in.</param>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ProcessLLMMessageCommand : IRequest<LLMResponse>
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();

    public BearerToken? AccessToken { get; set; }
    public Guid? AgentId { get; set; }
    public AssistantPageContext? PageContext { get; set; }
    public bool IsVoiceMode { get; set; }
}
