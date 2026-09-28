// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Exports;

public record SaveExportFormatOverrideCommand(string FormatKey, string PatchJson, bool IsEnabled, string? Note) : IRequest<ExportFormatOverrideResource>;
