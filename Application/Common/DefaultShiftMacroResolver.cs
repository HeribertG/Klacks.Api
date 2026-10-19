// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Common;

/// <summary>
/// Looks up the macro that shift-creation paths auto-assign when the caller left MacroId empty. This is
/// opt-out, not opt-in: a planner who removes the macro from a shift must have that choice respected on
/// every later edit, so this resolver is only ever consulted on create, never on update. Only the
/// function holders of category Shift qualify (Custom macros never do), and the partial unique index on
/// (category, type) guarantees at most one active macro per function, so the result is deterministic.
/// The SURCHARGE_STACKING_MODE setting (written by region-setup, e.g. KR/VN/PL) selects which standard
/// function is preferred: "additive" prefers the StandardAdditive macro and falls back to Standard,
/// anything else resolves the plain Standard macro.
/// </summary>
/// <param name="settingsRepository">Source of the active macro list and the stacking-mode setting.</param>
/// <param name="logger">Reports when no default macro is configured, so a shift created without one is traceable.</param>
public class DefaultShiftMacroResolver : IDefaultShiftMacroResolver
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<DefaultShiftMacroResolver> _logger;

    public DefaultShiftMacroResolver(ISettingsRepository settingsRepository, ILogger<DefaultShiftMacroResolver> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<Guid?> ResolveDefaultMacroIdAsync(CancellationToken cancellationToken = default)
    {
        var preferredFunction = await ResolvePreferredFunctionAsync();
        var macros = await _settingsRepository.GetMacroList();
        var shiftMacros = macros
            .Where(m => !m.IsDeleted && m.Category == MacroCategoryEnum.Shift)
            .ToList();

        var macroId = shiftMacros.FirstOrDefault(m => (MacroFunctionEnum)m.Type == preferredFunction)?.Id
            ?? shiftMacros.FirstOrDefault(m => (MacroFunctionEnum)m.Type == MacroFunctionEnum.Standard)?.Id;

        if (macroId == null)
        {
            _logger.LogInformation(
                "No default shift macro configured (no active macro of category Shift with function {Function} or Standard); the new shift keeps no macro",
                preferredFunction);
        }

        return macroId;
    }

    private async Task<MacroFunctionEnum> ResolvePreferredFunctionAsync()
    {
        var setting = await _settingsRepository.GetSettingNoTracking(SettingKeys.SurchargeStackingMode);
        return string.Equals(setting?.Value, SurchargeStackingModeValues.Additive, StringComparison.OrdinalIgnoreCase)
            ? MacroFunctionEnum.StandardAdditive
            : MacroFunctionEnum.Standard;
    }
}
