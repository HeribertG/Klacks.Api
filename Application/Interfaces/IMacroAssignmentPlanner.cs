// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Application.Interfaces;

public interface IMacroAssignmentPlanner
{
    Task<MacroAssignmentPreview> PreviewAssignAsync(
        MacroAssignmentTarget target, Guid holderId, Guid macroId, CancellationToken cancellationToken = default);

    Task<MacroRevertPreview> PreviewRevertAsync(MacroRevertRequest request, CancellationToken cancellationToken = default);
}
