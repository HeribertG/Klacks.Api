// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The live container children (child Works and Breaks) of one or several container Works, read without tracking so a caller
/// can check them before it stages anything. The default value holds no children.
/// </summary>
/// <param name="works">The live child Works</param>
/// <param name="breaks">The live child Breaks</param>

namespace Klacks.Api.Domain.Models.Schedules;

public readonly struct ContainerChildren
{
    private readonly IReadOnlyList<Work>? _works;
    private readonly IReadOnlyList<Break>? _breaks;

    public ContainerChildren(IReadOnlyList<Work> works, IReadOnlyList<Break> breaks)
    {
        _works = works;
        _breaks = breaks;
    }

    public static ContainerChildren None => default;

    public IReadOnlyList<Work> Works => _works ?? [];

    public IReadOnlyList<Break> Breaks => _breaks ?? [];
}
