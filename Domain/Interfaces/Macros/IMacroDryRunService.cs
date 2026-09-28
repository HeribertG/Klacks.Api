// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
