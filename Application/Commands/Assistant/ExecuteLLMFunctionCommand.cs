// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command for executing a named LLM skill function on behalf of a user.
/// </summary>
/// <param name="FunctionName">The skill function name to execute (may be a legacy alias)</param>
/// <param name="Parameters">Key-value map of function arguments passed by the LLM</param>
/// <param name="UserId">ID of the user triggering the function call</param>
/// <param name="UserRights">List of permission strings held by the user</param>
/// <param name="PageContext">Optional context about the current frontend page</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ExecuteLLMFunctionCommand : IRequest<LLMFunctionResult>
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> UserRights { get; set; } = new();
    public AssistantPageContext? PageContext { get; set; }
}
