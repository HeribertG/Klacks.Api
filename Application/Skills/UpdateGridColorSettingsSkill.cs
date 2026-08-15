// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes the colours the schedule grid is drawn in. Every supplied value is checked to be a hex
/// colour before anything is written, so one malformed value cannot leave the grid half-recoloured.
/// </summary>
/// <param name="backgroundColor">Background of an ordinary day.</param>
/// <param name="backgroundColorSaturday">Background of a Saturday.</param>
/// <param name="backgroundColorSunday">Background of a Sunday.</param>
/// <param name="backgroundColorHoliday">Background of a holiday.</param>
/// <param name="backgroundColorOfficialHoliday">Background of an official holiday.</param>
/// <param name="borderColor">Colour of the grid lines.</param>
/// <param name="borderEndMonthColor">Colour of the line that closes a month.</param>
/// <param name="focusBorderColor">Colour marking the selected cell.</param>
/// <param name="mainTextColor">Colour of the main text.</param>
/// <param name="subTextColor">Colour of the secondary text.</param>
/// <param name="foregroundColor">General foreground colour.</param>
/// <param name="evenMonthColor">Shade of even months.</param>
/// <param name="oddMonthColor">Shade of odd months.</param>
/// <param name="controlBackgroundColor">Background of the controls around the grid.</param>
/// <param name="headerBackgroundColor">Background of the header row.</param>
/// <param name="headerForegroundColor">Text colour of the header row.</param>
/// <param name="workChangeColor">Colour marking a correction.</param>
/// <param name="surchargeColor">Colour marking extra pay.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_grid_color_settings")]
public partial class UpdateGridColorSettingsSkill : SettingsWriterSkillBase
{
    public UpdateGridColorSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex HexColourPattern();

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var pending = new List<PendingSetting>();

        CollectText(pending, parameters, "backgroundColor", GridColorSettingKeys.BackgroundColor);
        CollectText(pending, parameters, "backgroundColorSaturday", GridColorSettingKeys.BackgroundColorSaturday);
        CollectText(pending, parameters, "backgroundColorSunday", GridColorSettingKeys.BackgroundColorSunday);
        CollectText(pending, parameters, "backgroundColorHoliday", GridColorSettingKeys.BackgroundColorHoliday);
        CollectText(pending, parameters, "backgroundColorOfficialHoliday", GridColorSettingKeys.BackgroundColorOfficialHoliday);
        CollectText(pending, parameters, "borderColor", GridColorSettingKeys.BorderColor);
        CollectText(pending, parameters, "borderEndMonthColor", GridColorSettingKeys.BorderEndMonthColor);
        CollectText(pending, parameters, "focusBorderColor", GridColorSettingKeys.FocusBorderColor);
        CollectText(pending, parameters, "mainTextColor", GridColorSettingKeys.MainTextColor);
        CollectText(pending, parameters, "subTextColor", GridColorSettingKeys.SubTextColor);
        CollectText(pending, parameters, "foregroundColor", GridColorSettingKeys.ForegroundColor);
        CollectText(pending, parameters, "evenMonthColor", GridColorSettingKeys.EvenMonthColor);
        CollectText(pending, parameters, "oddMonthColor", GridColorSettingKeys.OddMonthColor);
        CollectText(pending, parameters, "controlBackgroundColor", GridColorSettingKeys.ControlBackgroundColor);
        CollectText(pending, parameters, "headerBackgroundColor", GridColorSettingKeys.HeaderBackgroundColor);
        CollectText(pending, parameters, "headerForegroundColor", GridColorSettingKeys.HeaderForegroundColor);
        CollectText(pending, parameters, "workChangeColor", GridColorSettingKeys.WorkChangeColor);
        CollectText(pending, parameters, "surchargeColor", GridColorSettingKeys.SurchargeColor);

        var malformed = pending
            .Where(p => !HexColourPattern().IsMatch(p.Value))
            .Select(p => $"{p.ParameterName}='{p.Value}'")
            .ToList();

        if (malformed.Count > 0)
        {
            return SkillResult.Error(
                $"Not a hex colour: {string.Join(", ", malformed)}. Use #RGB, #RRGGBB or #RRGGBBAA.");
        }

        return await PersistAsync(pending, "Grid colour settings");
    }
}
