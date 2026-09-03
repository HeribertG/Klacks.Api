// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using System.Text;
using Klacks.Api.Data.Seed.Demo;

namespace Klacks.Api.Data.Seed
{
    public static class ShiftSeed
    {
        private const string ShiftTimeSqlFormat = "HH:mm:ss";

        private const string ShiftDateSqlFormat = "yyyy-MM-dd";

        private const string SqlNullLiteral = "NULL";

        private const string SqlTrueLiteral = "true";

        private const string SqlFalseLiteral = "false";

        private const int HoursPerDay = 24;

        private static readonly string user = "Anonymus";

        public static Dictionary<Guid, List<string>> ShiftGroupMappings { get; private set; } = new Dictionary<Guid, List<string>>();

        public static (string script, List<Guid> shiftIds) GenerateInsertScriptForShifts(string language = "de")
        {
            StringBuilder script = new StringBuilder();
            var shiftIds = new List<Guid>();
            ShiftGroupMappings.Clear();
            var baseDate = DemoOrderDefinitionFactory.DefaultBaseDate;
            var currentTime = DateTime.UtcNow;

            script.AppendLine("-- Shift Seed Data - Following Correct Workflow");
            script.AppendLine("-- Status: 0 = OriginalOrder, 1 = SealedOrder, 2 = OriginalShift, 3 = SplitShift");

            var nameRegistry = new DemoSeedNameRegistry(language);
            var definitionFactory = new DemoOrderDefinitionFactory(language, nameRegistry, baseDate);
            var definitions = definitionFactory.CreateShiftOrders()
                .GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<DemoOrderDefinition>)g.ToList());

            void TrackShiftGroups(Guid shiftId, IReadOnlyList<string> cantonNames)
            {
                ShiftGroupMappings[shiftId] = cantonNames.ToList();
            }

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
            foreach (var definition in definitions[DemoOrderCategory.SimpleShift])
            {
                var orderId = Guid.NewGuid(); // SealedOrder ID
                var originalShiftId = Guid.NewGuid(); // OriginalShift ID (Kopie)
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;

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
                    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(5))}', NULL, '{definition.Abbreviation}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(6))}',
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
                    '{originalShiftId}', false, '{definition.OriginalShiftDescription}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(7))}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(8))}', '{orderId}', '{definition.Abbreviation}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
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
                var definition = definitions[DemoOrderCategory.MorningShift][i - 1];
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;

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
                    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(10))}', NULL, '{definition.Abbreviation}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(11))}',
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
                    '{originalShiftId}', false, '{definition.OriginalShiftDescription}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(12))}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(13))}', '{orderId}', '{definition.Abbreviation}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
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
                var definition = definitions[DemoOrderCategory.DayShift][i - 1];
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;

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
                    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(15))}', NULL, '{definition.Abbreviation}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(16))}',
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
                    '{originalShiftId}', false, '{definition.OriginalShiftDescription}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(17))}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(18))}', '{orderId}', '{definition.Abbreviation}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
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
                var definition = definitions[DemoOrderCategory.NightShiftWeekday][i - 1];
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;

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
                    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(20))}', NULL, '{definition.Abbreviation}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(21))}',
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
                    '{originalShiftId}', false, '{definition.OriginalShiftDescription}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(22))}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(23))}', '{orderId}', '{definition.Abbreviation}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
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
                var definition = definitions[DemoOrderCategory.NightShiftWeekend][i - 1];
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;

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
                    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(25))}', NULL, '{definition.Abbreviation}', '00:00:00',
                    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
                );");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, assignedGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(26))}',
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
                    '{originalShiftId}', false, '{definition.OriginalShiftDescription}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
                    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
                    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
                    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(27))}', '{user}', NULL, '{user}',
                    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(28))}', '{orderId}', '{definition.Abbreviation}', '00:00:00',
                    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
                    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
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
                var definition = definitions[DemoOrderCategory.TwentyFourHourShift][i - 1];
                var employees = definition.SumEmployees;
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;
                var workflowGroups = definition.RootGroups;

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
    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(5))}', NULL, '{definition.Abbreviation}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
);");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, workflowGroups);

                // Step 2: Update OriginalOrder to SealedOrder (Status 0 -> 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1) - GLEICHER Datensatz!
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(6))}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: Create 3 SplitShift Children DIREKT (KEIN ROOT!)
                // WICHTIG: Beim Seeding gibt es KEINEN ROOT SplitShift!
                // Stattdessen: 3 eigenständige SplitShifts als Geschwister
                var split1Id = Guid.NewGuid();
                var uniqueNameFrüh = nameRegistry.UniqueName("Frühschicht-Teil", i);
                var uniqueAbbrFrüh = nameRegistry.UniqueAbbreviation("F", i);

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
    '{split1Id}', false, '{DemoOrderDescriptions.SplitMorning(language, employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameFrüh}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '15:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrFrüh}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split1Id, workflowGroups);

                var split2Id = Guid.NewGuid();
                var uniqueNameSpät = nameRegistry.UniqueName("Spätschicht-Teil", i);
                var uniqueAbbrSpät = nameRegistry.UniqueAbbreviation("S", i);

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
    '{split2Id}', false, '{DemoOrderDescriptions.SplitAfternoon(language, employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameSpät}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '23:00:00', '{baseDate:yyyy-MM-dd}', '15:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrSpät}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split2Id, workflowGroups);

                var split3Id = Guid.NewGuid();
                var uniqueNameNacht = nameRegistry.UniqueName("Nachtschicht-Teil", i);
                var uniqueAbbrNacht = nameRegistry.UniqueAbbreviation("N", i);

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
    '{split3Id}', false, '{DemoOrderDescriptions.SplitNight(language, employees)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNameNacht}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, true, false, 1, '00:00:00', '00:00:00',
    8, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, NULL,
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
                var definition = definitions[DemoOrderCategory.NightCutShift][i - 1];
                var startShift = FormatShiftTime(definition.StartShift);
                var endShift = FormatShiftTime(definition.EndShift);
                var untilDate = FormatUntilDate(definition.UntilDate);
                var assignedGroups = definition.RootGroups;
                var workflowGroups = definition.RootGroups;

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
    '{orderId}', false, '{definition.Description}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(30))}', NULL, '{definition.Abbreviation}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
);");

                shiftIds.Add(orderId);
                TrackShiftGroups(orderId, workflowGroups);

                // Step 2: Update to SealedOrder (Status 0 → 1)
                script.AppendLine($@"
-- Update to SealedOrder (Status = 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(31))}',
    current_user_updated = '{user}'
WHERE id = '{orderId}';");

                // Step 3: Create 2 SplitShifts - Split at 02:00 (AFTER midnight!)
                var split1Id = Guid.NewGuid();
                var uniqueNamePre = nameRegistry.UniqueName("Vor-Mitternacht-Teil", i);
                var uniqueAbbrPre = nameRegistry.UniqueAbbreviation("VM", i);

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
    '{split1Id}', false, '{DemoOrderDescriptions.PreMidnightPart(language)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNamePre}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '02:00:00', '{baseDate:yyyy-MM-dd}', '22:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    4, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrPre}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', 1, 0, 1, 2
);");

                shiftIds.Add(split1Id);
                TrackShiftGroups(split1Id, workflowGroups);

                var split2Id = Guid.NewGuid();
                var uniqueNamePost = nameRegistry.UniqueName("Nach-Mitternacht-Teil", i);
                var uniqueAbbrPost = nameRegistry.UniqueAbbreviation("NM", i);
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
    '{split2Id}', true, '{DemoOrderDescriptions.PostMidnightPart(language)}', 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{uniqueNamePost}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '06:00:00', '{nextDay:yyyy-MM-dd}', '02:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    4, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, NULL,
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
            var currentTime = DateTime.UtcNow;
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
    {container.WorkTime}, 1, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(1))}', NULL, '{container.Abbr}', '00:00:00',
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
            var currentTime = DateTime.UtcNow;

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
                        SELECT '{groupItemId}', NULL, g.id, '{shiftId}', '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', false
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
            var currentTime = DateTime.UtcNow;
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
    8, 1, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(1))}', NULL, '{abbr}', '00:00:00',
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
            var currentTime = DateTime.UtcNow;
            var baseDate = DemoOrderDefinitionFactory.DefaultBaseDate;

            script.AppendLine("\n-- TimeRange Shifts with Clients (100 Shifts PRO RootGroup = 400 total, 10-30 min WorkTime, 6-8h TimeRange)");
            script.AppendLine("-- WICHTIG: is_time_range=true, client_id wird per Subquery von Customer-Clients (type=2) geholt");
            script.AppendLine("-- Workflow: OriginalOrder (Status 0) -> SealedOrder (Status 1) -> OriginalShift (Status 2)");

            var nameRegistry = new DemoSeedNameRegistry(language);
            var definitionFactory = new DemoOrderDefinitionFactory(language, nameRegistry, baseDate);
            var definitions = definitionFactory.CreateTimeRangeOrders();

            var rootGroups = DemoOrderDefinitionFactory.RootGroups;

            for (int groupIndex = 0; groupIndex < rootGroups.Count; groupIndex++)
            {
                var rootGroup = rootGroups[groupIndex];

                script.AppendLine($"\n-- {DemoOrderDefinitionFactory.TimeRangeShiftsPerRootGroup} TimeRange Shifts für RootGroup: {rootGroup}");

                for (int i = 1; i <= DemoOrderDefinitionFactory.TimeRangeShiftsPerRootGroup; i++)
                {
                    var definition = definitions[(groupIndex * DemoOrderDefinitionFactory.TimeRangeShiftsPerRootGroup) + i - 1];
                    var orderId = Guid.NewGuid();
                    var originalShiftId = Guid.NewGuid();

                    var startShift = FormatShiftTime(definition.StartShift);
                    var endShift = FormatShiftTime(definition.EndShift);
                    var untilDate = FormatUntilDate(definition.UntilDate);
                    var crossesMidnight = definition.EndShift <= definition.StartShift;
                    var timeRangeHours = ((definition.EndShift.Hour - definition.StartShift.Hour) + HoursPerDay) % HoursPerDay;

                    script.AppendLine($@"
-- TimeRange Shift #{definition.Index} (WorkTime: {definition.WorkTimeMinutes} min = {definition.WorkTimeSqlLiteral} h, Range: {timeRangeHours}h, {(crossesMidnight ? "crosses midnight" : "daytime")})
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
    '{orderId}', false, '{definition.Description}',
    'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime)}', '{user}', NULL, '{user}',
    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(1))}', NULL, '{definition.Abbreviation}', '00:00:00',
    (SELECT id FROM public.client WHERE type = 2 AND is_deleted = false ORDER BY random() LIMIT 1),
    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
);");

                    shiftIds.Add(orderId);
                    ShiftGroupMappings[orderId] = new List<string> { rootGroup };

                    script.AppendLine($@"
-- Step 2: Update to SealedOrder (Status 0 -> 1)
UPDATE public.shift
SET status = 1,
    update_time = '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(2))}',
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
    '{originalShiftId}', false, '{definition.OriginalShiftDescription}',
    'a3edd3f5-c31c-4746-a9a0-c613d14ffd23', '{definition.Name}', NULL, NULL, 2,
    '00:00:00', '00:00:00', '{endShift}', '{definition.FromDate:yyyy-MM-dd}', '{startShift}', {untilDate},
    {SqlBool(definition.IsFriday)}, {SqlBool(definition.IsHoliday)}, {SqlBool(definition.IsMonday)}, {SqlBool(definition.IsSaturday)}, {SqlBool(definition.IsSunday)}, {SqlBool(definition.IsThursday)}, {SqlBool(definition.IsTuesday)}, {SqlBool(definition.IsWednesday)},
    {SqlBool(definition.IsWeekdayAndHoliday)}, false, {SqlBool(definition.IsTimeRange)}, {definition.Quantity}, '00:00:00', '00:00:00',
    {definition.WorkTimeSqlLiteral}, 0, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(3))}', '{user}', NULL, '{user}',
    NULL, false, '{SeedSqlTimestamp.ToLiteral(currentTime.AddMinutes(4))}', '{orderId}', '{definition.Abbreviation}', '00:00:00',
    (SELECT client_id FROM public.shift WHERE id = '{orderId}'),
    '00:00:00', {definition.SumEmployees}, 0, NULL, NULL
);");

                    shiftIds.Add(originalShiftId);
                    ShiftGroupMappings[originalShiftId] = new List<string> { rootGroup };
                }
            }

            return (script.ToString(), shiftIds);
        }

        private static string FormatShiftTime(TimeOnly value) => value.ToString(ShiftTimeSqlFormat, CultureInfo.InvariantCulture);

        private static string FormatUntilDate(DateOnly? value) =>
            value is { } date ? $"'{date.ToString(ShiftDateSqlFormat, CultureInfo.InvariantCulture)}'" : SqlNullLiteral;

        private static string SqlBool(bool value) => value ? SqlTrueLiteral : SqlFalseLiteral;
    }
}
