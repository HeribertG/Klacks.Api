// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Application.Interfaces;

public interface IMacroRevertPlanner
{
    Task<MacroRevertPreview> PreviewRevertAsync(MacroRevertRequest request, CancellationToken cancellationToken = default);
}
