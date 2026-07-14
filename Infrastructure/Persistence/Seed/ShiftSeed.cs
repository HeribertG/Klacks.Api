// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;

namespace Klacks.Api.Data.Seed
{
    public static class ShiftSeed
    {
        private static readonly string user = "Anonymus";

        public static Dictionary<Guid, List<string>> ShiftGroupMappings { get; private set; } = new Dictionary<Guid, List<string>>();

        public static (string script, List<Guid> shiftIds) GenerateInsertScriptForShifts(string language = "de")
        {
            StringBuilder script = new StringBuilder();
            var shiftIds = new List<Guid>();
            ShiftGroupMappings.Clear();
            var usedNames = new HashSet<string>();
            var usedAbbreviations = new HashSet<string>();

            var baseDate = new DateOnly(2025, 1, 1);
            var currentTime = DateTime.Now;
            var random = Random.Shared;

            script.AppendLine("-- Shift Seed Data - Following Correct Workflow");
            script.AppendLine("-- Status: 0 = OriginalOrder, 1 = SealedOrder, 2 = OriginalShift, 3 = SplitShift");

            // Available ROOT GROUP names for assignment (4 Root Groups)
            var availableRootGroups = new[] {
                "Westschweiz",           // Root 1: GE, VD, NE, JU, FR
                "Deutschschweiz Zürich",    // Root 2: ZH, AG
                "Deutschschweiz Mitte",     // Root 3: BE, SO, BS, BL
                "Deutschschweiz Ost"        // Root 4: LU, SG, etc.
            };

            List<string> GetRandomRootGroups(int count)
            {
                return availableRootGroups.OrderBy(x => random.Next()).Take(count).ToList();
            }

            void TrackShiftGroups(Guid shiftId, List<string> cantonNames)
            {
                ShiftGroupMappings[shiftId] = cantonNames;
            }

            var nameTranslations = new Dictionary<string, Dictionary<string, string>>
            {
                ["Morgenschicht"] = new() { ["ar"] = "وردية صباحية", ["he"] = "משמרת בוקר", ["ja"] = "朝番" },
                ["Tagschicht"] = new() { ["ar"] = "وردية نهارية", ["he"] = "משמרת יום", ["ja"] = "日勤" },
                ["Nachtdienst Mo-Fr"] = new() { ["ar"] = "مناوبة ليلية الإثنين-الجمعة", ["he"] = "תורנות לילה ב׳-ו׳", ["ja"] = "夜勤 月-金" },
                ["Nachtdienst Sa-So"] = new() { ["ar"] = "مناوبة ليلية السبت-الأحد", ["he"] = "תורנות לילה ש׳-א׳", ["ja"] = "夜勤 土-日" },
                ["24h-Schichtdienst"] = new() { ["ar"] = "دوام 24 ساعة", ["he"] = "משמרת 24 שעות", ["ja"] = "24時間勤務" },
                ["Frühschicht-Teil"] = new() { ["ar"] = "جزء الوردية الصباحية", ["he"] = "חלק משמרת בוקר", ["ja"] = "早番部分" },
                ["Spätschicht-Teil"] = new() { ["ar"] = "جزء الوردية المسائية", ["he"] = "חלק משמרת ערב", ["ja"] = "遅番部分" },
                ["Nachtschicht-Teil"] = new() { ["ar"] = "جزء الوردية الليلية", ["he"] = "חלק משמרת לילה", ["ja"] = "夜勤部分" },
                ["Nachtschicht-Teilung"] = new() { ["ar"] = "تقسيم الوردية الليلية", ["he"] = "פיצול משמרת לילה", ["ja"] = "夜勤分割" },
                ["Vor-Mitternacht-Teil"] = new() { ["ar"] = "جزء ما قبل منتصف الليل", ["he"] = "חלק לפני חצות", ["ja"] = "深夜前部分" },
                ["Nach-Mitternacht-Teil"] = new() { ["ar"] = "جزء ما بعد منتصف الليل", ["he"] = "חלק אחרי חצות", ["ja"] = "深夜後部分" },
            };

            var abbrTranslations = new Dictionary<string, Dictionary<string, string>>
            {
                ["MOR"] = new() { ["ar"] = "صبح", ["he"] = "בקר", ["ja"] = "朝" },
                ["TAG"] = new() { ["ar"] = "نهر", ["he"] = "יום", ["ja"] = "日" },
                ["NMF"] = new() { ["ar"] = "لنج", ["he"] = "לבו", ["ja"] = "夜月金" },
                ["NSS"] = new() { ["ar"] = "لسح", ["he"] = "לשא", ["ja"] = "夜土日" },
                ["24H"] = new() { ["ar"] = "24س", ["he"] = "24ש", ["ja"] = "24時" },
                ["F"] = new() { ["ar"] = "ص", ["he"] = "ב", ["ja"] = "早" },
                ["S"] = new() { ["ar"] = "م", ["he"] = "ע", ["ja"] = "遅" },
                ["N"] = new() { ["ar"] = "ل", ["he"] = "ל", ["ja"] = "夜" },
                ["NCT"] = new() { ["ar"] = "تل", ["he"] = "פל", ["ja"] = "夜分" },
                ["VM"] = new() { ["ar"] = "قم", ["he"] = "לח", ["ja"] = "前" },
                ["NM"] = new() { ["ar"] = "بم", ["he"] = "אח", ["ja"] = "後" },
            };

            string GetUniqueName(string baseName, int counter)
            {
                if (nameTranslations.TryGetValue(baseName, out var translations) && translations.TryGetValue(language, out var translated))
                {
                    baseName = translated;
                }

                var name = counter == 1 ? baseName : $"{baseName} {counter}";
                while (usedNames.Contains(name))
                {
                    counter++;
                    name = $"{baseName} {counter}";
                }
                usedNames.Add(name);
                return name;
            }

            string GetUniqueAbbreviation(string baseAbbr, int counter)
            {
                if (abbrTranslations.TryGetValue(baseAbbr, out var translations) && translations.TryGetValue(language, out var translated))
                {
                    baseAbbr = translated;
                }

                var abbr = counter == 1 ? baseAbbr : $"{baseAbbr}{counter}";
                while (usedAbbreviations.Contains(abbr))
                {
                    counter++;
                    abbr = $"{baseAbbr}{counter}";
                }
                usedAbbreviations.Add(abbr);
                return abbr;
            }

            string SimpleShiftDescription(string name, int employees) => language switch
            {
                "ar" => $"{name} بواقع {employees} موظف",
                "he" => $"{name} עם {employees} עובדים",
                "ja" => $"{name}(担当者{employees}名)",
                _ => $"{name} mit {employees} Mitarbeiter(n)",
            };

            string MorningShiftDescription(int employees) => language switch
            {
                "ar" => $"وردية صباحية لمدة 6 ساعات - {employees} موظف لكل وردية",
                "he" => $"משמרת בוקר בת 6 שעות - {employees} עובדים למשמרת",
                "ja" => $"6時間の朝番 - 1シフトあたり{employees}名",
                _ => $"6-Stunden Morgenschicht - {employees} Mitarbeiter pro Schicht",
            };

            string DayShiftDescription(int employees) => language switch
            {
                "ar" => $"وردية نهارية الإثنين-الجمعة مع استراحة غداء ساعة - {employees} موظف لكل وردية",
                "he" => $"משמרת יום ב׳-ו׳ עם הפסקת צהריים של שעה - {employees} עובדים למשמרת",
                "ja" => $"月-金の日勤(昼休憩1時間あり) - 1シフトあたり{employees}名",
                _ => $"Tagschicht Mo-Fr mit 1h Mittagspause - {employees} Mitarbeiter pro Schicht",
            };

            string NightShiftMfDescription() => language switch
            {
                "ar" => "مناوبة ليلية الإثنين-الجمعة - موظف واحد لكل مناوبة",
                "he" => "תורנות לילה ב׳-ו׳ - עובד אחד למשמרת",
                "ja" => "月-金の夜勤 - 1シフトあたり1名",
                _ => "Nachtdienst Mo-Fr - 1 Mitarbeiter pro Schicht",
            };

            string NightShiftSsDescription() => language switch
            {
                "ar" => "مناوبة ليلية السبت-الأحد - موظف واحد لكل مناوبة",
                "he" => "תורנות לילה ש׳-א׳ - עובד אחד למשמרת",
                "ja" => "土-日の夜勤 - 1シフトあたり1名",
                _ => "Nachtdienst Sa-So - 1 Mitarbeiter pro Schicht",
            };

            string TwentyFourHourDescription(int employees) => language switch
            {
                "ar" => $"دوام 24 ساعة - {employees} موظف لكل دوام",
                "he" => $"משמרת 24 שעות - {employees} עובדים למשמרת",
                "ja" => $"24時間勤務 - 1シフトあたり{employees}名",
                _ => $"24-Stunden Schichtdienst - {employees} Mitarbeiter pro Schicht",
            };

            string SplitMorningDescription(int employees) => language switch
            {
                "ar" => $"الوردية الصباحية - {employees} موظف",
                "he" => $"משמרת בוקר - {employees} עובדים",
                "ja" => $"早番 - {employees}名",
                _ => $"Frühschicht - {employees} Mitarbeiter",
            };

            string SplitAfternoonDescription(int employees) => language switch
            {
                "ar" => $"الوردية المسائية - {employees} موظف",
                "he" => $"משמרת ערב - {employees} עובדים",
                "ja" => $"遅番 - {employees}名",
                _ => $"Spätschicht - {employees} Mitarbeiter",
            };

            string SplitNightDescription(int employees) => language switch
            {
                "ar" => $"الوردية الليلية - {employees} موظف",
                "he" => $"משמרת לילה - {employees} עובדים",
                "ja" => $"夜勤 - {employees}名",
                _ => $"Nachtschicht - {employees} Mitarbeiter",
            };

            string NightCutDescription() => language switch
            {
                "ar" => "الوردية الليلية 22:00-06:00 مع تقسيم عند 02:00",
                "he" => "משמרת לילה 22:00-06:00 עם פיצול בשעה 02:00",
                "ja" => "夜勤 22:00-06:00(02:00で分割)",
                _ => "Nachtschicht 22:00-06:00 mit Teilung bei 02:00",
            };

            string PreMidnightDescription() => language switch
            {
                "ar" => "الجزء قبل منتصف الليل (22:00-02:00)",
                "he" => "החלק שלפני חצות (22:00-02:00)",
                "ja" => "深夜0時前の部分 (22:00-02:00)",
                _ => "Teil VOR Mitternacht (22:00-02:00)",
            };

            string PostMidnightDescription() => language switch
            {
                "ar" => "الجزء بعد منتصف الليل (02:00-06:00)",
                "he" => "החלק שאחרי חצות (02:00-06:00)",
                "ja" => "深夜0時後の部分 (02:00-06:00)",
                _ => "Teil NACH Mitternacht (02:00-06:00)",
            };

            // 1. Create 10 simple OriginalOrder shifts (Status = 0) → SealedOrder (Status = 1) → OriginalShift (Status = 2)
            // VERDOPPELT: 5 → 10, Root Groups zugewiesen
            var simpleShiftsAr = new[]
            {
                new { Name = "الوردية الصباحية", Abbr = "صب", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "الوردية المسائية", Abbr = "مسا", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
                new { Name = "الوردية الليلية", Abbr = "ليل", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "دوام نهاري", Abbr = "نهر", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "الاستعداد", Abbr = "است", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
            };
            var simpleShiftsHe = new[]
            {
                new { Name = "משמרת בוקר", Abbr = "בק", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "משמרת ערב", Abbr = "ער", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
                new { Name = "משמרת לילה", Abbr = "לי", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "תורנות יום", Abbr = "יום", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "כוננות", Abbr = "כון", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
            };
            var simpleShiftsJa = new[]
            {
                new { Name = "早番", Abbr = "早", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "遅番", Abbr = "遅", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
                new { Name = "夜勤", Abbr = "夜", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "日勤", Abbr = "日", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "待機", Abbr = "待", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
            };
            var simpleShiftsDe = new[]
            {
                new { Name = "Frühschicht", Abbr = "FS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "Spätschicht", Abbr = "SS", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
                new { Name = "Nachtschicht", Abbr = "NS", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "Tagdienst", Abbr = "TAG", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
                new { Name = "Bereitschaft", Abbr = "BD", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
            };
            var simpleShiftsBase = language switch
            {
                "ar" => simpleShiftsAr,
                "he" => simpleShiftsHe,
                "ja" => simpleShiftsJa,
                _ => simpleShiftsDe,
            };
            var simpleShifts = simpleShiftsBase.Concat(simpleShiftsBase).ToArray();

            script.AppendLine("\n-- 1. Simple Shifts (Workflow: OriginalOrder → SealedOrder → OriginalShift) - VERDOPPELT mit Root Groups");
            foreach (var shift in simpleShifts)
            {
                var orderId = Guid.NewGuid(); // SealedOrder ID
                var originalShiftId = Guid.NewGuid(); // OriginalShift ID (Kopie)
                var uniqueName = GetUniqueName(shift.Name, 1);
                var uniqueAbbr = GetUniqueAbbreviation(shift.Abbr, 1);
                var assignedGroups = GetRandomRootGroups(random.Next(1, 3)); // 1-2 random ROOT GROUPS

                // Step 1: Create OriginalOrder (Status = 0)
                script.AppendLine($@"
-- OriginalOrder (Status = 0)
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, '{SimpleShiftDescription(shift.Name, shift.Employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueName}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{shift.End}', '{baseDate:yyyy-MM-dd}', '{shift.Start}', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, {(shift.IsTimeRange ? "true" : "false")}, 1, '00:00:00', '00:00:00',
                    {shift.WorkTime}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbr}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {shift.Employees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(6):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: Create OriginalShift (Status = 2) - 1:1 Kopie mit GLEICHEN Groups!
                script.AppendLine($@"
-- OriginalShift (Status = 2) - Verplanbare Kopie mit GLEICHEN Groups!
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, '{SimpleShiftDescription(shift.Name, shift.Employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueName}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{shift.End}', '{baseDate:yyyy-MM-dd}', '{shift.Start}', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, {(shift.IsTimeRange ? "true" : "false")}, 1, '00:00:00', '00:00:00',
                    {shift.WorkTime}, 0, '{currentTime.AddMinutes(7):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(8):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbr}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {shift.Employees}, 0, NULL, NULL
                );");

                shiftIds.Add(originalShiftId);
                TrackShiftGroups(originalShiftId, assignedGroups); // ✅ GLEICHE Groups!
            }

            // 2. Create 40 Morgenschichten (6 Stunden) - Workflow: OriginalOrder → SealedOrder → OriginalShift
            // VERDOPPELT: 20 → 40, Root Groups zugewiesen
            script.AppendLine("\n-- 2. Morning Shifts (Workflow: OriginalOrder → SealedOrder → OriginalShift) - VERDOPPELT mit Root Groups");
            for (int i = 1; i <= 40; i++)
            {
                var orderId = Guid.NewGuid();
                var originalShiftId = Guid.NewGuid();
                var startHour = random.Next(5, 8);
                var endHour = startHour + 6;
                var employees = (i <= 3) ? 2 : 1;
                var uniqueNameMorning = GetUniqueName("Morgenschicht", i);
                var uniqueAbbrMorning = GetUniqueAbbreviation("MOR", i);
                var assignedGroups = GetRandomRootGroups(random.Next(1, 3)); // 1-2 random ROOT GROUPS

                // Step 1: OriginalOrder (Status = 0)
                script.AppendLine($@"
-- OriginalOrder (Status = 0)
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, '{MorningShiftDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameMorning}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    6, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(10):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrMorning}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(11):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: OriginalShift (Status = 2) mit GLEICHEN Groups
                script.AppendLine($@"
-- OriginalShift (Status = 2) - Verplanbare Kopie mit GLEICHEN Groups
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, '{MorningShiftDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameMorning}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    6, 0, '{currentTime.AddMinutes(12):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(13):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrMorning}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(originalShiftId);
                TrackShiftGroups(originalShiftId, assignedGroups); // ✅ GLEICHE Groups!
            }

            // 3. Create 60 Tagschichten Mo-Fr 08:00-17:00 - Workflow: OriginalOrder → SealedOrder → OriginalShift
            // VERDOPPELT: 30 → 60, Root Groups zugewiesen
            script.AppendLine("\n-- 3. Day Shifts Mo-Fr (Workflow: OriginalOrder → SealedOrder → OriginalShift) - VERDOPPELT mit Root Groups");
            for (int i = 1; i <= 60; i++)
            {
                var orderId = Guid.NewGuid();
                var originalShiftId = Guid.NewGuid();
                var employees = (i <= 5) ? 2 : 1;
                var uniqueNameDay = GetUniqueName("Tagschicht", i);
                var uniqueAbbrDay = GetUniqueAbbreviation("TAG", i + 100);
                var assignedGroups = GetRandomRootGroups(random.Next(1, 3)); // 1-2 random ROOT GROUPS

                // Step 1: OriginalOrder (Status = 0)
                script.AppendLine($@"
-- OriginalOrder (Status = 0)
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, '{DayShiftDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameDay}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '17:00:00', '{baseDate:yyyy-MM-dd}', '08:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(15):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrDay}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(16):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: OriginalShift (Status = 2) mit GLEICHEN Groups
                script.AppendLine($@"
-- OriginalShift (Status = 2) - Verplanbare Kopie mit GLEICHEN Groups
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, '{DayShiftDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameDay}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '17:00:00', '{baseDate:yyyy-MM-dd}', '08:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime.AddMinutes(17):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(18):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrDay}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(originalShiftId);
                TrackShiftGroups(originalShiftId, assignedGroups); // ✅ GLEICHE Groups!
            }

            // 4. Create 40 Nachtdienste Mo-Fr 23:00-07:00 - Workflow: OriginalOrder → SealedOrder → OriginalShift
            // VERDOPPELT: 20 → 40, Root Groups zugewiesen
            script.AppendLine("\n-- 4. Night Shifts Mo-Fr (Workflow: OriginalOrder → SealedOrder → OriginalShift) - VERDOPPELT mit Root Groups");
            for (int i = 1; i <= 40; i++)
            {
                var orderId = Guid.NewGuid();
                var originalShiftId = Guid.NewGuid();
                var uniqueNameNightMF = GetUniqueName("Nachtdienst Mo-Fr", i);
                var uniqueAbbrNightMF = GetUniqueAbbreviation("NMF", i);
                var assignedGroups = GetRandomRootGroups(random.Next(1, 3)); // 1-2 random ROOT GROUPS

                // Step 1: OriginalOrder (Status = 0)
                script.AppendLine($@"
-- OriginalOrder (Status = 0)
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, '{NightShiftMfDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNightMF}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, false, true, false, false, true, true, false,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(20):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightMF}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(21):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: OriginalShift (Status = 2) mit GLEICHEN Groups
                script.AppendLine($@"
-- OriginalShift (Status = 2) - Verplanbare Kopie mit GLEICHEN Groups
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, '{NightShiftMfDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNightMF}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, false, true, false, false, true, true, false,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime.AddMinutes(22):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(23):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrNightMF}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(originalShiftId);
                TrackShiftGroups(originalShiftId, assignedGroups); // ✅ GLEICHE Groups!
            }

            // 5. Create 40 Nachtdienste Sa-So 23:00-07:00 - Workflow: OriginalOrder → SealedOrder → OriginalShift
            // VERDOPPELT: 20 → 40, Root Groups zugewiesen
            script.AppendLine("\n-- 5. Night Shifts Sa-So (Workflow: OriginalOrder → SealedOrder → OriginalShift) - VERDOPPELT mit Root Groups");
            for (int i = 1; i <= 40; i++)
            {
                var orderId = Guid.NewGuid();
                var originalShiftId = Guid.NewGuid();
                var uniqueNameNightSS = GetUniqueName("Nachtdienst Sa-So", i);
                var uniqueAbbrNightSS = GetUniqueAbbreviation("NSS", i);
                var assignedGroups = GetRandomRootGroups(random.Next(1, 3)); // 1-2 random ROOT GROUPS

                // Step 1: OriginalOrder (Status = 0)
                script.AppendLine($@"
-- OriginalOrder (Status = 0)
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, '{NightShiftSsDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNightSS}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    false, false, false, true, true, false, false, false,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(25):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightSS}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(26):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: OriginalShift (Status = 2) mit GLEICHEN Groups
                script.AppendLine($@"
-- OriginalShift (Status = 2) - Verplanbare Kopie mit GLEICHEN Groups
INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, '{NightShiftSsDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNightSS}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    false, false, false, true, true, false, false, false,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime.AddMinutes(27):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(28):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrNightSS}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(originalShiftId);
                TrackShiftGroups(originalShiftId, assignedGroups); // ✅ GLEICHE Groups!
            }

            // 6. Create 20 sealed shifts with splits to demonstrate the complete workflow
            // VERDOPPELT: 10 → 20, Root Groups zugewiesen
            script.AppendLine("\n-- 6. Example Sealed Shifts with Splits (Status 0 -> 1 -> 3 Children) - SEEDING VERSION - VERDOPPELT mit Root Groups");
            script.AppendLine("-- WICHTIG: Beim Seeding werden KEINE ROOT SplitShifts erstellt!");
            script.AppendLine("-- Stattdessen: 3 eigenständige SplitShifts als Geschwister (parent_id=NULL, root_id=SealedOrder, lft=1, rgt=2)");
            for (int i = 1; i <= 20; i++)
            {
                var orderId = Guid.NewGuid(); // EINE ID für Order (wird von Status 0 -> 1 updated)
                var employees = (i <= 2) ? 2 : 1;

                var uniqueName24h = GetUniqueName("24h-Schichtdienst", i);
                var uniqueAbbr24h = GetUniqueAbbreviation("24H", i);

                // WICHTIG: ALLE Shifts in diesem Workflow bekommen die GLEICHEN Root Groups!
                var workflowGroups = GetRandomRootGroups(random.Next(1, 2)); // 1 ROOT GROUP for workflow consistency

                // Step 1: Create OriginalOrder (Status = 0)
                script.AppendLine($@"
-- OriginalOrder (Status = 0)
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{orderId}', false, '{TwentyFourHourDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueName24h}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    24, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbr24h}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', {employees}, 0, NULL, NULL
);");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, workflowGroups);

                // Step 2: Update OriginalOrder to SealedOrder (Status 0 -> 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1) - GLEICHER Datensatz!
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(6):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: Create 3 SplitShift Children DIREKT (KEIN ROOT!)
                // WICHTIG: Beim Seeding gibt es KEINEN ROOT SplitShift!
                // Stattdessen: 3 eigenständige SplitShifts als Geschwister
                var split1Id = Guid.NewGuid();
                var uniqueNameFrüh = GetUniqueName("Frühschicht-Teil", i);
                var uniqueAbbrFrüh = GetUniqueAbbreviation("F", i);

                script.AppendLine($@"
-- SplitShift 1 (Status = 3) - Frühschicht 07:00-15:00 - Eigenständig, KEIN Parent!
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split1Id}', false, '{SplitMorningDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameFrüh}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '15:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrFrüh}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split1Id, workflowGroups);

                var split2Id = Guid.NewGuid();
                var uniqueNameSpät = GetUniqueName("Spätschicht-Teil", i);
                var uniqueAbbrSpät = GetUniqueAbbreviation("S", i);

                script.AppendLine($@"
-- SplitShift 2 (Status = 3) - Spätschicht 15:00-23:00 - Eigenständig, KEIN Parent!
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split2Id}', false, '{SplitAfternoonDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameSpät}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '23:00:00', '{baseDate:yyyy-MM-dd}', '15:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrSpät}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split2Id, workflowGroups);

                var split3Id = Guid.NewGuid();
                var uniqueNameNacht = GetUniqueName("Nachtschicht-Teil", i);
                var uniqueAbbrNacht = GetUniqueAbbreviation("N", i);

                script.AppendLine($@"
-- SplitShift 3 (Status = 3) - Night shift 23:00-07:00 - Independent, NO Parent!
-- CuttingAfterMidnight = false because StartShift (23:00) is BEFORE midnight
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split3Id}', false, '{SplitNightDescription(employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNacht}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, true, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrNacht}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split3Id, workflowGroups);
            }

            // 7. Create 10 night shifts with REAL after-midnight splits
            // Example: Shift 22:00-06:00 split at 02:00
            // Part 1: 22:00-02:00 → CuttingAfterMidnight = false (StartShift BEFORE midnight)
            // Part 2: 02:00-06:00 → CuttingAfterMidnight = true (StartShift AFTER midnight)
            script.AppendLine("\n-- 7. Night shifts with REAL after-midnight splits");
            script.AppendLine("-- IMPORTANT: CuttingAfterMidnight = true ONLY when StartShift is AFTER midnight!");
            for (int i = 1; i <= 10; i++)
            {
                var orderId = Guid.NewGuid();
                var uniqueNameNightCut = GetUniqueName("Nachtschicht-Teilung", i);
                var uniqueAbbrNightCut = GetUniqueAbbreviation("NCT", i);
                var workflowGroups = GetRandomRootGroups(random.Next(1, 2));

                // Step 1: Create OriginalOrder (Status = 0) - Nachtschicht 22:00-06:00
                script.AppendLine($@"
-- OriginalOrder (Status = 0) - Nachtschicht 22:00-06:00
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{orderId}', false, '{NightCutDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNightCut}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '06:00:00', '{baseDate:yyyy-MM-dd}', '22:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(30):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightCut}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', 1, 0, NULL, NULL
);");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, workflowGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(31):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: Create 2 SplitShifts - Split at 02:00 (AFTER midnight!)
                var split1Id = Guid.NewGuid();
                var uniqueNamePre = GetUniqueName("Vor-Mitternacht-Teil", i);
                var uniqueAbbrPre = GetUniqueAbbreviation("VM", i);

                script.AppendLine($@"
-- SplitShift 1 (Status = 3) - BEFORE midnight: 22:00-02:00
-- CuttingAfterMidnight = false because StartShift (22:00) is BEFORE midnight
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split1Id}', false, '{PreMidnightDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNamePre}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '02:00:00', '{baseDate:yyyy-MM-dd}', '22:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    4, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrPre}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', 1, 0, 1, 2
);");

                shiftIds.Add(split1Id);
                TrackShiftGroups(split1Id, workflowGroups);

                var split2Id = Guid.NewGuid();
                var uniqueNamePost = GetUniqueName("Nach-Mitternacht-Teil", i);
                var uniqueAbbrPost = GetUniqueAbbreviation("NM", i);
                var nextDay = baseDate.AddDays(1);

                script.AppendLine($@"
-- SplitShift 2 (Status = 3) - AFTER midnight: 02:00-06:00
-- CuttingAfterMidnight = TRUE because StartShift (02:00) is AFTER midnight!
-- IMPORTANT: from_date is +1 day because this part occurs on the next calendar day!
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split2Id}', true, '{PostMidnightDescription()}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNamePost}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '06:00:00', '{nextDay:yyyy-MM-dd}', '02:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    4, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrPost}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', 1, 0, 1, 2
);");

                shiftIds.Add(split2Id);
                TrackShiftGroups(split2Id, workflowGroups);
            }

            return (script.ToString(), shiftIds);
        }

        public static (string script, List<Guid> containerIds) GenerateContainerTemplates(string language = "de")
        {
            StringBuilder script = new StringBuilder();
            var containerIds = new List<Guid>();
            var random = Random.Shared;
            var currentTime = DateTime.Now;
            var baseDate = new DateOnly(2025, 1, 1);

            script.AppendLine("\n-- Container Seed Data (ShiftType = IsContainer)");
            script.AppendLine("-- Containers with Root Groups for Container Templates");

            var availableRootGroups = new[] {
                "Westschweiz",
                "Deutschschweiz Zürich",
                "Deutschschweiz Mitte",
                "Deutschschweiz Ost"
            };

            List<string> GetRandomRootGroups(int count)
            {
                return availableRootGroups.OrderBy(x => random.Next()).Take(count).ToList();
            }

            void TrackContainerGroups(Guid containerId, List<string> groupNames)
            {
                ShiftGroupMappings[containerId] = groupNames;
            }

            var containersAr = new[]
            {
                new { Name = "صباح الإثنين-الجمعة", Abbr = "صب-نج", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "صباح السبت-الأحد", Abbr = "صب-سح", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "ظهر الإثنين-الجمعة", Abbr = "ظه-نج", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "ظهر السبت-الأحد", Abbr = "ظه-سح", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "مساء الإثنين-الجمعة", Abbr = "مس-نج", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "مساء السبت-الأحد", Abbr = "مس-سح", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "ليل الإثنين-الجمعة", Abbr = "لي-نج", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "ليل السبت-الأحد", Abbr = "لي-سح", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "يوم كامل الإثنين-الجمعة", Abbr = "يك-نج", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "يوم كامل السبت-الأحد", Abbr = "يك-سح", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "مبكر الإثنين-الجمعة", Abbr = "مب-نج", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "مبكر السبت-الأحد", Abbr = "مب-سح", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "متأخر الإثنين-الجمعة", Abbr = "مت-نج", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "متأخر السبت-الأحد", Abbr = "مت-سح", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "صباحًا يوميًا", Abbr = "صب-يو", Start = "08:00:00", End = "12:00:00", WorkTime = 4, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "بعد الظهر يوميًا", Abbr = "بظ-يو", Start = "12:00:00", End = "17:00:00", WorkTime = 5, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "ساعات المكتب الإثنين-الجمعة", Abbr = "سم-نج", Start = "08:00:00", End = "17:00:00", WorkTime = 9, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "عطلة نهاية الأسبوع", Abbr = "عط", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "الإثنين-الأربعاء", Abbr = "ن-رب", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = false, Fri = false, Sat = false, Sun = false },
                new { Name = "الخميس-الجمعة", Abbr = "خ-ج", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = true, Fri = true, Sat = false, Sun = false },
            };
            var containersHe = new[]
            {
                new { Name = "בוקר ב׳-ו׳", Abbr = "בק-בו", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "בוקר ש׳-א׳", Abbr = "בק-שא", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "צהריים ב׳-ו׳", Abbr = "צה-בו", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "צהריים ש׳-א׳", Abbr = "צה-שא", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "ערב ב׳-ו׳", Abbr = "ער-בו", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "ערב ש׳-א׳", Abbr = "ער-שא", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "לילה ב׳-ו׳", Abbr = "לי-בו", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "לילה ש׳-א׳", Abbr = "לי-שא", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "יום שלם ב׳-ו׳", Abbr = "יש-בו", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "יום שלם ש׳-א׳", Abbr = "יש-שא", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "מוקדם ב׳-ו׳", Abbr = "מק-בו", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "מוקדם ש׳-א׳", Abbr = "מק-שא", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "מאוחר ב׳-ו׳", Abbr = "מא-בו", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "מאוחר ש׳-א׳", Abbr = "מא-שא", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "לפני הצהריים יומי", Abbr = "לצ-יו", Start = "08:00:00", End = "12:00:00", WorkTime = 4, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "אחר הצהריים יומי", Abbr = "אצ-יו", Start = "12:00:00", End = "17:00:00", WorkTime = 5, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "שעות משרד ב׳-ו׳", Abbr = "שמ-בו", Start = "08:00:00", End = "17:00:00", WorkTime = 9, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "סוף שבוע", Abbr = "סש", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "שני-רביעי", Abbr = "ב-ד", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = false, Fri = false, Sat = false, Sun = false },
                new { Name = "חמישי-שישי", Abbr = "ה-ו", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = true, Fri = true, Sat = false, Sun = false },
            };
            var containersJa = new[]
            {
                new { Name = "朝 月-金", Abbr = "朝月金", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "朝 土-日", Abbr = "朝土日", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "昼 月-金", Abbr = "昼月金", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "昼 土-日", Abbr = "昼土日", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "夕方 月-金", Abbr = "夕月金", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "夕方 土-日", Abbr = "夕土日", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "夜 月-金", Abbr = "夜月金", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "夜 土-日", Abbr = "夜土日", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "終日 月-金", Abbr = "終月金", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "終日 土-日", Abbr = "終土日", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "早番 月-金", Abbr = "早月金", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "早番 土-日", Abbr = "早土日", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "遅番 月-金", Abbr = "遅月金", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "遅番 土-日", Abbr = "遅土日", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "午前 毎日", Abbr = "午前", Start = "08:00:00", End = "12:00:00", WorkTime = 4, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "午後 毎日", Abbr = "午後", Start = "12:00:00", End = "17:00:00", WorkTime = 5, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "事務時間 月-金", Abbr = "事月金", Start = "08:00:00", End = "17:00:00", WorkTime = 9, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "週末", Abbr = "週末", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "月-水", Abbr = "月水", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = false, Fri = false, Sat = false, Sun = false },
                new { Name = "木-金", Abbr = "木金", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = true, Fri = true, Sat = false, Sun = false },
            };
            var containersDe = new[]
            {
                new { Name = "Morgen Mo-Fr", Abbr = "MO-MF", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Morgen Sa-So", Abbr = "MO-SS", Start = "06:00:00", End = "14:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Mittag Mo-Fr", Abbr = "MI-MF", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Mittag Sa-So", Abbr = "MI-SS", Start = "10:00:00", End = "18:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Abend Mo-Fr", Abbr = "AB-MF", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Abend Sa-So", Abbr = "AB-SS", Start = "14:00:00", End = "22:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Nacht Mo-Fr", Abbr = "NA-MF", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Nacht Sa-So", Abbr = "NA-SS", Start = "22:00:00", End = "06:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Ganztag Mo-Fr", Abbr = "GZ-MF", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Ganztag Sa-So", Abbr = "GZ-SS", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Früh Mo-Fr", Abbr = "FR-MF", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Früh Sa-So", Abbr = "FR-SS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Spät Mo-Fr", Abbr = "SP-MF", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Spät Sa-So", Abbr = "SP-SS", Start = "15:00:00", End = "23:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Vormittag täglich", Abbr = "VM-TG", Start = "08:00:00", End = "12:00:00", WorkTime = 4, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "Nachmittag täglich", Abbr = "NM-TG", Start = "12:00:00", End = "17:00:00", WorkTime = 5, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "Bürozeiten Mo-Fr", Abbr = "BZ-MF", Start = "08:00:00", End = "17:00:00", WorkTime = 9, Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Wochenende", Abbr = "WE", Start = "06:00:00", End = "22:00:00", WorkTime = 16, Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Montag-Mittwoch", Abbr = "MO-MI", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = true, Tue = true, Wed = true, Thu = false, Fri = false, Sat = false, Sun = false },
                new { Name = "Donnerstag-Freitag", Abbr = "DO-FR", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Mon = false, Tue = false, Wed = false, Thu = true, Fri = true, Sat = false, Sun = false },
            };
            var containers = language switch
            {
                "ar" => containersAr,
                "he" => containersHe,
                "ja" => containersJa,
                _ => containersDe,
            };

            var containerDescriptionPrefix = language switch
            {
                "ar" => "حاوية",
                "he" => "מאגר",
                "ja" => "コンテナ",
                _ => "Container für",
            };

            foreach (var container in containers)
            {
                var containerId = Guid.NewGuid();
                var assignedGroups = GetRandomRootGroups(random.Next(1, 4));

                script.AppendLine($@"
-- Container: {container.Name}
-- CuttingAfterMidnight = false because Containers are not SplitShifts
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{containerId}', false, '{containerDescriptionPrefix} {container.Name}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{container.Name}', NULL, NULL, 2,
    '00:00:00', '00:00:00', '{container.End}', '{baseDate:yyyy-MM-dd}', '{container.Start}', NULL,
    {(container.Fri ? "true" : "false")}, false, {(container.Mon ? "true" : "false")}, {(container.Sat ? "true" : "false")}, {(container.Sun ? "true" : "false")}, {(container.Thu ? "true" : "false")}, {(container.Tue ? "true" : "false")}, {(container.Wed ? "true" : "false")},
    false, false, false, 1, '00:00:00', '00:00:00',
    {container.WorkTime}, 1, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(1):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{container.Abbr}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', 1, 0, NULL, NULL
);");

                containerIds.Add(containerId);
                TrackContainerGroups(containerId, assignedGroups);
            }

            return (script.ToString(), containerIds);
        }

        public static string GenerateInsertScriptForShiftGroupItems(List<Guid> shiftIds)
        {
            StringBuilder script = new StringBuilder();
            var currentTime = DateTime.Now;

            script.AppendLine("\n-- GroupItem entries for Shift-Group assignments");
            script.AppendLine("-- WICHTIG: SealedOrder -> OriginalShift -> SplitShift haben die GLEICHEN Groups!");

            foreach (var shiftId in shiftIds)
            {
                // Hole die zugewiesenen Gruppen aus dem Mapping
                if (!ShiftGroupMappings.TryGetValue(shiftId, out var cantonNames))
                {
                    continue; // Shift hat keine Gruppen-Zuordnung
                }

                foreach (var cantonName in cantonNames)
                {
                    var groupItemId = Guid.NewGuid();

                    script.AppendLine($@"INSERT INTO public.group_item (id, client_id, group_id, shift_id, create_time, current_user_created, is_deleted)
                        SELECT '{groupItemId}', NULL, g.id, '{shiftId}', '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', false
                        FROM public.""group"" g
                        WHERE g.name = '{cantonName}' AND g.is_deleted = false
                        LIMIT 1;");
                }
            }

            return script.ToString();
        }

        public static (string script, List<Guid> containerIds) GenerateContainers(string language = "de")
        {
            StringBuilder script = new StringBuilder();
            var containerIds = new List<Guid>();
            var random = Random.Shared;
            var currentTime = DateTime.Now;
            var baseDate = new DateOnly(2025, 1, 1);

            script.AppendLine("\n-- Container (Tag, Abend, Nacht) - 20 pro RootGroup = 240 total");
            script.AppendLine("-- shift_type = 1 (IsContainer), status = 2 (OriginalShift)");

            var availableRootGroups = new[] {
                "Westschweiz",
                "Deutschschweiz Zürich",
                "Deutschschweiz Mitte",
                "Deutschschweiz Ost"
            };

            var containerTypes = language switch
            {
                "ar" => new[]
                {
                    new { Name = "نهار", Start = "06:00:00", End = "18:00:00" },
                    new { Name = "مساء", Start = "14:00:00", End = "22:00:00" },
                    new { Name = "ليل", Start = "22:00:00", End = "06:00:00" },
                },
                "he" => new[]
                {
                    new { Name = "יום", Start = "06:00:00", End = "18:00:00" },
                    new { Name = "ערב", Start = "14:00:00", End = "22:00:00" },
                    new { Name = "לילה", Start = "22:00:00", End = "06:00:00" },
                },
                "ja" => new[]
                {
                    new { Name = "昼", Start = "06:00:00", End = "18:00:00" },
                    new { Name = "夕", Start = "14:00:00", End = "22:00:00" },
                    new { Name = "夜", Start = "22:00:00", End = "06:00:00" },
                },
                _ => new[]
                {
                    new { Name = "Tag", Start = "06:00:00", End = "18:00:00" },
                    new { Name = "Abend", Start = "14:00:00", End = "22:00:00" },
                    new { Name = "Nacht", Start = "22:00:00", End = "06:00:00" },
                },
            };
            var containerNamePrefix = language switch
            {
                "ar" => "حاوية",
                "he" => "מאגר",
                "ja" => "コンテナ",
                _ => "Container",
            };

            var containersPerGroupPerType = 20;
            int globalCounter = 1;

            foreach (var rootGroup in availableRootGroups)
            {
                foreach (var containerType in containerTypes)
                {
                    script.AppendLine($"\n-- {containersPerGroupPerType} {containerType.Name}-Container für RootGroup: {rootGroup}");

                    for (int i = 1; i <= containersPerGroupPerType; i++)
                    {
                        var containerId = Guid.NewGuid();
                        var name = $"{containerNamePrefix} {containerType.Name} {globalCounter}";
                        var abbr = $"C{containerType.Name[0]}{globalCounter}";

                        script.AppendLine($@"
-- Container #{globalCounter}: {name} ({containerType.Start}-{containerType.End})
-- CuttingAfterMidnight = false because Containers are not SplitShifts
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{containerId}', false, '{(language switch { "ar" => $"حاوية {containerType.Name} لـ {rootGroup}", "he" => $"מאגר {containerType.Name} עבור {rootGroup}", "ja" => $"コンテナ {containerType.Name}({rootGroup})", _ => $"Container {containerType.Name} für {rootGroup}" })}',
    'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{name}', NULL, NULL, 2,
    '00:00:00', '00:00:00', '{containerType.End}', '{baseDate:yyyy-MM-dd}', '{containerType.Start}', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 1, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(1):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{abbr}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', 1, 0, NULL, NULL
);");

                        containerIds.Add(containerId);
                        ShiftGroupMappings[containerId] = new List<string> { rootGroup };
                        globalCounter++;
                    }
                }
            }

            return (script.ToString(), containerIds);
        }

        public static (string script, List<Guid> shiftIds) GenerateTimeRangeShiftsWithClients(string language = "de")
        {
            StringBuilder script = new StringBuilder();
            var shiftIds = new List<Guid>();
            var random = Random.Shared;
            var currentTime = DateTime.Now;
            var baseDate = new DateOnly(2025, 1, 1);

            script.AppendLine("\n-- TimeRange Shifts with Clients (100 Shifts PRO RootGroup = 400 total, 10-30 min WorkTime, 6-8h TimeRange)");
            script.AppendLine("-- WICHTIG: is_time_range=true, client_id wird per Subquery von Customer-Clients (type=2) geholt");
            script.AppendLine("-- Workflow: OriginalOrder (Status 0) -> SealedOrder (Status 1) -> OriginalShift (Status 2)");

            var availableRootGroups = new[] {
                "Westschweiz",
                "Deutschschweiz Zürich",
                "Deutschschweiz Mitte",
                "Deutschschweiz Ost"
            };

            var shiftsPerGroup = 100;

            for (int groupIndex = 0; groupIndex < availableRootGroups.Length; groupIndex++)
            {
                var rootGroup = availableRootGroups[groupIndex];

                script.AppendLine($"\n-- {shiftsPerGroup} TimeRange Shifts für RootGroup: {rootGroup}");

                for (int i = 1; i <= shiftsPerGroup; i++)
                {
                    var orderId = Guid.NewGuid();
                    var originalShiftId = Guid.NewGuid();

                    var workTimeMinutes = random.Next(10, 31);
                    var workTimeDecimal = Math.Round(workTimeMinutes / 60.0, 4);

                    var timeRangeHours = random.Next(6, 9);

                    // 50% der Shifts gehen über Mitternacht
                    bool crossesMidnight = random.Next(100) < 50;
                    int startHour, endHour;

                    if (crossesMidnight)
                    {
                        // Mitternachtsüberschreitung: Start zwischen 18:00 und 23:00
                        startHour = random.Next(18, 24);
                        endHour = (startHour + timeRangeHours) % 24;
                    }
                    else
                    {
                        // Normale Shifts: Start zwischen 06:00 und (19 - timeRangeHours)
                        startHour = random.Next(6, Math.Max(7, 19 - timeRangeHours));
                        endHour = startHour + timeRangeHours;
                    }

                    var shiftNumber = (groupIndex * shiftsPerGroup) + i;
                    var name = language switch
                    {
                        "ar" => $"وردية زمنية {shiftNumber}",
                        "he" => $"משמרת גמישה {shiftNumber}",
                        "ja" => $"フレックス勤務{shiftNumber}",
                        _ => $"TimeRange-Shift {shiftNumber}",
                    };
                    var abbr = language switch
                    {
                        "ar" => $"وز{shiftNumber}",
                        "he" => $"מג{shiftNumber}",
                        "ja" => $"フレ{shiftNumber}",
                        _ => $"TR{shiftNumber}",
                    };

                    script.AppendLine($@"
-- TimeRange Shift #{shiftNumber} (WorkTime: {workTimeMinutes} min = {workTimeDecimal} h, Range: {timeRangeHours}h, {(crossesMidnight ? "crosses midnight" : "daytime")})
-- Step 1: Create OriginalOrder (Status = 0) with random Customer client
-- CuttingAfterMidnight = false because OriginalOrder is not a SplitShift
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{orderId}', false, '{(language switch { "ar" => $"وردية زمنية بمدة عمل {workTimeMinutes} دقيقة ضمن نافذة {timeRangeHours} ساعات{(crossesMidnight ? " (عبر منتصف الليل)" : "")}", "he" => $"משמרת גמישה באורך {workTimeMinutes} דקות בחלון של {timeRangeHours} שעות{(crossesMidnight ? " (חוצה חצות)" : "")}", "ja" => $"作業時間{workTimeMinutes}分、{timeRangeHours}時間の枠内のフレックス勤務{(crossesMidnight ? "(深夜0時をまたぐ)" : "")}", _ => $"TimeRange Shift {workTimeMinutes} Minuten Arbeitszeit in {timeRangeHours}h Zeitfenster{(crossesMidnight ? " (über Mitternacht)" : "")}" })}',
    'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{name}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, true, 1, '00:00:00', '00:00:00',
    {workTimeDecimal.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(1):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{abbr}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', 1, 0, NULL, NULL
);");

                    shiftIds.Add(orderId);
                    ShiftGroupMappings[orderId] = new List<string> { rootGroup };

                    script.AppendLine($@"
-- Step 2: Update to SealedOrder (Status 0 -> 1)
UPDATE public.shift
SET status = 1,
    update_time = '{currentTime.AddMinutes(2):yyyy-MM-dd HH:mm:ss.ffffff}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                    script.AppendLine($@"
-- Step 3: Create OriginalShift (Status = 2) - 1:1 copy with SAME Groups and client_id!
-- CuttingAfterMidnight = false because OriginalShift is not a SplitShift
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_and_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{originalShiftId}', false, 'TimeRange Shift {workTimeMinutes} Minuten Arbeitszeit in {timeRangeHours}h Zeitfenster{(crossesMidnight ? " (über Mitternacht)" : "")}',
    'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{name}', NULL, NULL, 2,
    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, true, 1, '00:00:00', '00:00:00',
    {workTimeDecimal.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 0, '{currentTime.AddMinutes(3):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(4):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{abbr}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', 1, 0, NULL, NULL
);");

                    shiftIds.Add(originalShiftId);
                    ShiftGroupMappings[originalShiftId] = new List<string> { rootGroup };
                }
            }

            return (script.ToString(), shiftIds);
        }
    }
}
