// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Macros;

public record PostCommand(MacroResource model, MacroOrigin Origin = MacroOrigin.User) : IRequest<MacroResource>;
