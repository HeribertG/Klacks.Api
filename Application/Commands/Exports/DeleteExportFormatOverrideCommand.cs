// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Exports;

public record DeleteExportFormatOverrideCommand(string FormatKey) : IRequest<bool>;
