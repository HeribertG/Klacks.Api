// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Setting keys of the schedule grid appearance. The frontend writes these keys from
/// grid-constants.ts; they are mirrored here so skills can read and write them without literals.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GridColorSettingKeys
{
    public const string BackgroundColor = "BACKGROUND_COLOR_KEY";
    public const string BackgroundColorSaturday = "BACKGROUND_COLOR_SATURDAY_KEY";
    public const string BackgroundColorSunday = "BACKGROUND_COLOR_SUNDAY_KEY";
    public const string BackgroundColorHoliday = "BACKGROUND_COLOR_HOLIDAY_KEY";
    public const string BackgroundColorOfficialHoliday = "BACKGROUND_COLOR_OFFICIALLY_KEY";

    public const string BorderColor = "BORDER_COLOR_KEY";
    public const string BorderEndMonthColor = "BORDER_END_MONTH_COLOR_KEY";
    public const string FocusBorderColor = "FOCUS_BORDER_COLOR_KEY";

    public const string MainTextColor = "MAIN_TEXT_COLOR_KEY";
    public const string SubTextColor = "SUB_TEXT_COLOR_KEY";
    public const string ForegroundColor = "FOREGROUND_COLOR_KEY";

    public const string EvenMonthColor = "EVEN_MONTH_COLOR_KEY";
    public const string OddMonthColor = "ODD_MONTH_COLOR_KEY";

    public const string ControlBackgroundColor = "CONTROL_BACKGROUND_COLOR_KEY";
    public const string HeaderBackgroundColor = "HEADER_BACKGROUND_COLOR_KEY";
    public const string HeaderForegroundColor = "HEADER_FOREGROUND_COLOR_KEY";

    public const string WorkChangeColor = "WORK_CHANGE_COLOR_KEY";
    public const string SurchargeColor = "SURCHARGE_COLOR_KEY";
}
