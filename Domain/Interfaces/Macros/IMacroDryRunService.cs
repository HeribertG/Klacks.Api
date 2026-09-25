// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroDryRunService
{
    Task<MacroDryRunResult> RunAsync(
        MacroAssignmentTarget target,
        IReadOnlyList<MacroDryRunHolder> holders,
        CancellationToken cancellationToken = default);
}
