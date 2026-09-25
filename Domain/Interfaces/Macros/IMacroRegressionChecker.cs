// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroRegressionChecker
{
    MacroRegressionResult Check(string originalContent, string copyContent, CancellationToken cancellationToken = default);
}
