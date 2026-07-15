// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Common;

/// <summary>
/// Looks up the macro that shift-creation paths auto-assign when the caller left MacroId empty. This is
/// opt-out, not opt-in: a planner who removes the macro from a shift must have that choice respected on
/// every later edit, so this resolver is only ever consulted on create, never on update. The
/// SURCHARGE_STACKING_MODE setting (written by region-setup, e.g. KR/VN/PL) selects which standard
/// function is preferred: "additive" prefers the StandardAdditive macro and falls back to Standard,
/// anything else resolves the plain Standard macro.
/// </summary>
/// <param name="settingsRepository">Source of the active macro list and the stacking-mode setting.</param>
public class DefaultShiftMacroResolver : IDefaultShiftMacroResolver
{
    private readonly ISettingsRepository _settingsRepository;

    public DefaultShiftMacroResolver(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Guid?> ResolveDefaultMacroIdAsync(CancellationToken cancellationToken = default)
    {
        var preferredFunction = await ResolvePreferredFunctionAsync();
        var macros = await _settingsRepository.GetMacroList();
        var shiftMacros = macros.Where(m => m.Category == MacroCategoryEnum.Shift).ToList();

        return shiftMacros.FirstOrDefault(m => (MacroFunctionEnum)m.Type == preferredFunction)?.Id
            ?? shiftMacros.FirstOrDefault(m => (MacroFunctionEnum)m.Type == MacroFunctionEnum.Standard)?.Id;
    }

    private async Task<MacroFunctionEnum> ResolvePreferredFunctionAsync()
    {
        var setting = await _settingsRepository.GetSettingNoTracking(SettingKeys.SurchargeStackingMode);
        return string.Equals(setting?.Value, SurchargeStackingModeValues.Additive, StringComparison.OrdinalIgnoreCase)
            ? MacroFunctionEnum.StandardAdditive
            : MacroFunctionEnum.Standard;
    }
}
