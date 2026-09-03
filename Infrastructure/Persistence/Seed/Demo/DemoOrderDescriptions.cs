// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the localized description text of every demo order category.
/// </summary>
/// <param name="language">Two-letter language code selecting the wording</param>
/// <param name="employees">Number of employees the described shift asks for</param>

namespace Klacks.Api.Data.Seed.Demo;

public static class DemoOrderDescriptions
{
    public static string SimpleShift(string language, string name, int employees) => language switch
    {
        "ar" => $"{name} بواقع {employees} موظف",
        "he" => $"{name} עם {employees} עובדים",
        "ja" => $"{name}(担当者{employees}名)",
        _ => $"{name} mit {employees} Mitarbeiter(n)",
    };

    public static string MorningShift(string language, int employees) => language switch
    {
        "ar" => $"وردية صباحية لمدة 6 ساعات - {employees} موظف لكل وردية",
        "he" => $"משמרת בוקר בת 6 שעות - {employees} עובדים למשמרת",
        "ja" => $"6時間の朝番 - 1シフトあたり{employees}名",
        _ => $"6-Stunden Morgenschicht - {employees} Mitarbeiter pro Schicht",
    };

    public static string DayShift(string language, int employees) => language switch
    {
        "ar" => $"وردية نهارية الإثنين-الجمعة مع استراحة غداء ساعة - {employees} موظف لكل وردية",
        "he" => $"משמרת יום ב׳-ו׳ עם הפסקת צהריים של שעה - {employees} עובדים למשמרת",
        "ja" => $"月-金の日勤(昼休憩1時間あり) - 1シフトあたり{employees}名",
        _ => $"Tagschicht Mo-Fr mit 1h Mittagspause - {employees} Mitarbeiter pro Schicht",
    };

    public static string NightShiftWeekday(string language) => language switch
    {
        "ar" => "مناوبة ليلية الإثنين-الجمعة - موظف واحد لكل مناوبة",
        "he" => "תורנות לילה ב׳-ו׳ - עובד אחד למשמרת",
        "ja" => "月-金の夜勤 - 1シフトあたり1名",
        _ => "Nachtdienst Mo-Fr - 1 Mitarbeiter pro Schicht",
    };

    public static string NightShiftWeekend(string language) => language switch
    {
        "ar" => "مناوبة ليلية السبت-الأحد - موظف واحد لكل مناوبة",
        "he" => "תורנות לילה ש׳-א׳ - עובד אחד למשמרת",
        "ja" => "土-日の夜勤 - 1シフトあたり1名",
        _ => "Nachtdienst Sa-So - 1 Mitarbeiter pro Schicht",
    };

    public static string TwentyFourHourShift(string language, int employees) => language switch
    {
        "ar" => $"دوام 24 ساعة - {employees} موظف لكل دوام",
        "he" => $"משמרת 24 שעות - {employees} עובדים למשמרת",
        "ja" => $"24時間勤務 - 1シフトあたり{employees}名",
        _ => $"24-Stunden Schichtdienst - {employees} Mitarbeiter pro Schicht",
    };

    public static string SplitMorning(string language, int employees) => language switch
    {
        "ar" => $"الوردية الصباحية - {employees} موظف",
        "he" => $"משמרת בוקר - {employees} עובדים",
        "ja" => $"早番 - {employees}名",
        _ => $"Frühschicht - {employees} Mitarbeiter",
    };

    public static string SplitAfternoon(string language, int employees) => language switch
    {
        "ar" => $"الوردية المسائية - {employees} موظف",
        "he" => $"משמרת ערב - {employees} עובדים",
        "ja" => $"遅番 - {employees}名",
        _ => $"Spätschicht - {employees} Mitarbeiter",
    };

    public static string SplitNight(string language, int employees) => language switch
    {
        "ar" => $"الوردية الليلية - {employees} موظف",
        "he" => $"משמרת לילה - {employees} עובדים",
        "ja" => $"夜勤 - {employees}名",
        _ => $"Nachtschicht - {employees} Mitarbeiter",
    };

    public static string NightCutShift(string language) => language switch
    {
        "ar" => "الوردية الليلية 22:00-06:00 مع تقسيم عند 02:00",
        "he" => "משמרת לילה 22:00-06:00 עם פיצול בשעה 02:00",
        "ja" => "夜勤 22:00-06:00(02:00で分割)",
        _ => "Nachtschicht 22:00-06:00 mit Teilung bei 02:00",
    };

    public static string PreMidnightPart(string language) => language switch
    {
        "ar" => "الجزء قبل منتصف الليل (22:00-02:00)",
        "he" => "החלק שלפני חצות (22:00-02:00)",
        "ja" => "深夜0時前の部分 (22:00-02:00)",
        _ => "Teil VOR Mitternacht (22:00-02:00)",
    };

    public static string PostMidnightPart(string language) => language switch
    {
        "ar" => "الجزء بعد منتصف الليل (02:00-06:00)",
        "he" => "החלק שאחרי חצות (02:00-06:00)",
        "ja" => "深夜0時後の部分 (02:00-06:00)",
        _ => "Teil NACH Mitternacht (02:00-06:00)",
    };

    public static string TimeRangeShift(string language, int workTimeMinutes, int timeRangeHours, bool crossesMidnight) => language switch
    {
        "ar" => $"وردية زمنية بمدة عمل {workTimeMinutes} دقيقة ضمن نافذة {timeRangeHours} ساعات{(crossesMidnight ? " (عبر منتصف الليل)" : string.Empty)}",
        "he" => $"משמרת גמישה באורך {workTimeMinutes} דקות בחלון של {timeRangeHours} שעות{(crossesMidnight ? " (חוצה חצות)" : string.Empty)}",
        "ja" => $"作業時間{workTimeMinutes}分、{timeRangeHours}時間の枠内のフレックス勤務{(crossesMidnight ? "(深夜0時をまたぐ)" : string.Empty)}",
        _ => TimeRangeShiftGerman(workTimeMinutes, timeRangeHours, crossesMidnight),
    };

    public static string TimeRangeShiftGerman(int workTimeMinutes, int timeRangeHours, bool crossesMidnight) =>
        $"TimeRange Shift {workTimeMinutes} Minuten Arbeitszeit in {timeRangeHours}h Zeitfenster{(crossesMidnight ? " (über Mitternacht)" : string.Empty)}";
}
