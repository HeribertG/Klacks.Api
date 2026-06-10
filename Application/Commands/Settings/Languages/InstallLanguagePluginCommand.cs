// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Languages;

public record InstallLanguagePluginCommand(string Code) : IRequest<bool>;
