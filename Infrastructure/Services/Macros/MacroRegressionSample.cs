// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One input of the macro regression grid.
/// </summary>
/// <param name="Description">Human-readable description used in deviation reports</param>
/// <param name="Data">The macro inputs bound into both scripts</param>

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Infrastructure.Services.Macros;

public record MacroRegressionSample(string Description, MacroData Data);
