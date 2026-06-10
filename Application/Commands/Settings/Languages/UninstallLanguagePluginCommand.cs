// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Languages;

public record UninstallLanguagePluginCommand(string Code) : IRequest<bool>;
