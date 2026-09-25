// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Application.Interfaces;

public interface IMacroRevertPlanner
{
    Task<MacroRevertPreview> PreviewRevertAsync(MacroRevertRequest request, CancellationToken cancellationToken = default);
}
