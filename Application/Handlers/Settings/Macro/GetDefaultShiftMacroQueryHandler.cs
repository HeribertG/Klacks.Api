// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Macros;

/// <summary>
/// Returns the macro a new shift receives when the caller does not choose one, so the shift editor can
/// preselect exactly what the backend would assign on create. The backend resolver stays the single
/// source of truth; the editor never derives the default from category and function itself.
/// </summary>
/// <param name="defaultShiftMacroResolver">Resolves the default Shift-category macro, or null when none is configured.</param>
public class GetDefaultShiftMacroQueryHandler : IRequestHandler<GetDefaultShiftMacroQuery, DefaultShiftMacroResource>
{
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;

    public GetDefaultShiftMacroQueryHandler(IDefaultShiftMacroResolver defaultShiftMacroResolver)
    {
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
    }

    public async Task<DefaultShiftMacroResource> Handle(GetDefaultShiftMacroQuery request, CancellationToken cancellationToken)
    {
        var macroId = await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken);
        return new DefaultShiftMacroResource { MacroId = macroId };
    }
}
