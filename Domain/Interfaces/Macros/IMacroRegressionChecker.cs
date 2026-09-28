// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroRegressionChecker
{
    MacroRegressionResult Check(string originalContent, string copyContent, CancellationToken cancellationToken = default);
}
