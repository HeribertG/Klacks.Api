// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Application.Interfaces;

public interface IMacroAssignPlanner
{
    Task<MacroAssignmentPreview> PreviewAssignAsync(
        MacroAssignmentTarget target, Guid holderId, Guid macroId, CancellationToken cancellationToken = default);
}
