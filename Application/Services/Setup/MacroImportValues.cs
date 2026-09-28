// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired Macro row for the region-setup entity import (K20 macros section).
/// </summary>
public sealed record MacroImportValues(
    string Name,
    string Content,
    MacroCategoryEnum Category,
    MacroFunctionEnum Function);
