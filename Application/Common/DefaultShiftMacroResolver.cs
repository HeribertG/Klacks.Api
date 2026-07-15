// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Common;

/// <summary>
/// Looks up the macro flagged as the default for category Shift so shift-creation paths can
/// auto-assign it when the caller left MacroId empty. This is opt-out, not opt-in: a planner who
/// removes the macro from a shift must have that choice respected on every later edit, so this
/// resolver is only ever consulted on create, never on update.
/// </summary>
/// <param name="settingsRepository">Source of the active macro list.</param>
public class DefaultShiftMacroResolver : IDefaultShiftMacroResolver
{
    private readonly ISettingsRepository _settingsRepository;

    public DefaultShiftMacroResolver(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Guid?> ResolveDefaultMacroIdAsync(CancellationToken cancellationToken = default)
    {
        var macros = await _settingsRepository.GetMacroList();
        return macros
            .FirstOrDefault(m => m.Category == MacroCategoryEnum.Shift
                                  && (MacroFunctionEnum)m.Type == MacroFunctionEnum.Standard)
            ?.Id;
    }
}
