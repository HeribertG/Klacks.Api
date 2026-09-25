// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a calculation macro. ByAssistant tells who edits it: false (default) for the admin REST path, where an
/// assistant-owned macro becomes a user macro, true for the assistant's update skill, which keeps the origin.
/// </summary>
/// <param name="model">The macro resource with id, name, content (script), type and description</param>
/// <param name="ByAssistant">True when the assistant's skill performs the update</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Macros;

public record PutCommand(MacroResource model, bool ByAssistant = false) : IRequest<MacroResource>;
