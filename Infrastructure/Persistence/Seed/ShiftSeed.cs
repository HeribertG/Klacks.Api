using System.Text;

namespace Klacks.Api.Data.Seed
{
    public static class ShiftSeed
    {
        private static readonly string user = "Anonymus";

        public static Dictionary<Guid, List<string>> ShiftGroupMappings { get; private set; } = new Dictionary<Guid, List<string>>();

        public static (string script, List<Guid> shiftIds) GenerateInsertScriptForShifts()
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
                "Deutschweiz Zürich",    // Root 2: ZH, AG
                "Deutschweiz Mitte",     // Root 3: BE, SO, BS, BL
                "Deutschweiz Ost"        // Root 4: LU, SG, etc.
            };

            List<string> GetRandomRootGroups(int count)
            {
                return availableRootGroups.OrderBy(x => random.Next()).Take(count).ToList();
            }

            void TrackShiftGroups(Guid shiftId, List<string> cantonNames)
            {
                ShiftGroupMappings[shiftId] = cantonNames;
            }

            string GetUniqueName(string baseName, int counter)
            {
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
                var abbr = counter == 1 ? baseAbbr : $"{baseAbbr}{counter}";
                while (usedAbbreviations.Contains(abbr))
                {
                    counter++;
                    abbr = $"{baseAbbr}{counter}";
                }
                usedAbbreviations.Add(abbr);
                return abbr;
            }

            // 1. Create 10 simple OriginalOrder shifts (Status = 0) → SealedOrder (Status = 1) → OriginalShift (Status = 2)
            // VERDOPPELT: 5 → 10, Root Groups zugewiesen
            var simpleShifts = new[]
            {
                new { Name = "Frühschicht", Abbr = "FS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Spätschicht", Abbr = "SS", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, CuttingAfterMidnight = false },
                new { Name = "Nachtschicht", Abbr = "NS", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = true },
                new { Name = "Tagdienst", Abbr = "TAG", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Bereitschaft", Abbr = "BD", Start = "00:00:00", End = "00:00:00", WorkTime = 24, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Frühschicht", Abbr = "FS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Spätschicht", Abbr = "SS", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, CuttingAfterMidnight = false },
                new { Name = "Nachtschicht", Abbr = "NS", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = true },
                new { Name = "Tagdienst", Abbr = "TAG", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Bereitschaft", Abbr = "BD", Start = "00:00:00", End = "00:00:00", WorkTime = 24, Employees = 1, CuttingAfterMidnight = false }
            };

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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', {(shift.CuttingAfterMidnight ? "true" : "false")}, '{shift.Name} mit {shift.Employees} Mitarbeiter(n)', '00000000-0000-0000-0000-000000000000', '{uniqueName}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{shift.End}', '{baseDate:yyyy-MM-dd}', '{shift.Start}', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    {shift.WorkTime}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbr}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', {(shift.CuttingAfterMidnight ? "true" : "false")}, '{shift.Name} mit {shift.Employees} Mitarbeiter(n)', '00000000-0000-0000-0000-000000000000', '{uniqueName}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{shift.End}', '{baseDate:yyyy-MM-dd}', '{shift.Start}', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    {shift.WorkTime}, 0, '{currentTime.AddMinutes(7):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(8):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbr}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, '6-Stunden Morgenschicht - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameMorning}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    6, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(10):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrMorning}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, '6-Stunden Morgenschicht - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameMorning}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    6, 0, '{currentTime.AddMinutes(12):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(13):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrMorning}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', false, 'Tagschicht Mo-Fr mit 1h Mittagspause - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameDay}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '17:00:00', '{baseDate:yyyy-MM-dd}', '08:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(15):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrDay}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', false, 'Tagschicht Mo-Fr mit 1h Mittagspause - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameDay}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '17:00:00', '{baseDate:yyyy-MM-dd}', '08:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime.AddMinutes(17):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(18):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrDay}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', true, 'Nachtdienst Mo-Fr - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightMF}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, false, true, false, false, true, true, false,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(20):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightMF}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', true, 'Nachtdienst Mo-Fr - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightMF}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, false, true, false, false, true, true, false,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime.AddMinutes(22):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(23):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrNightMF}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{orderId}', true, 'Nachtdienst Sa-So - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightSS}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    false, false, false, true, true, false, false, false,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(25):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightSS}', '00:00:00', NULL,
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
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalShiftId}', true, 'Nachtdienst Sa-So - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightSS}', NULL, NULL, 2,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    false, false, false, true, true, false, false, false,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime.AddMinutes(27):yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(28):yyyy-MM-dd HH:mm:ss.ffffff}', '{orderId}', '{uniqueAbbrNightSS}', '00:00:00', NULL,
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
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{orderId}', true, '24-Stunden Schichtdienst - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueName24h}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    24, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbr24h}', '00:00:00', NULL,
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
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split1Id}', false, 'Frühschicht - {employees} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameFrüh}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '15:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrFrüh}', '00:00:00', NULL,
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
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split2Id}', false, 'Spätschicht - {employees} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameSpät}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '23:00:00', '{baseDate:yyyy-MM-dd}', '15:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrSpät}', '00:00:00', NULL,
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split2Id, workflowGroups);

                var split3Id = Guid.NewGuid();
                var uniqueNameNacht = GetUniqueName("Nachtschicht-Teil", i);
                var uniqueAbbrNacht = GetUniqueAbbreviation("N", i);

                script.AppendLine($@"
-- SplitShift 3 (Status = 3) - Nachtschicht 23:00-07:00 - Eigenständig, KEIN Parent!
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{split3Id}', true, 'Nachtschicht - {employees} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameNacht}', NULL, '{orderId}', 3,
    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
    true, true, true, true, true, true, true, true,
    false, true, false, 1, '00:00:00', '00:00:00',
    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, NULL,
    NULL, false, NULL, '{orderId}', '{uniqueAbbrNacht}', '00:00:00', NULL,
    '00:00:00', {employees}, 0, 1, 2
);");

                TrackShiftGroups(split3Id, workflowGroups);
            }

            return (script.ToString(), shiftIds);
        }

        public static (string script, List<Guid> containerIds) GenerateContainerTemplates()
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
                "Deutschweiz Zürich",
                "Deutschweiz Mitte",
                "Deutschweiz Ost"
            };

            List<string> GetRandomRootGroups(int count)
            {
                return availableRootGroups.OrderBy(x => random.Next()).Take(count).ToList();
            }

            void TrackContainerGroups(Guid containerId, List<string> groupNames)
            {
                ShiftGroupMappings[containerId] = groupNames;
            }

            var containers = new[]
            {
                new { Name = "Morgen Mo-Fr", Abbr = "MO-MF", Start = "06:00:00", End = "14:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Morgen Sa-So", Abbr = "MO-SS", Start = "06:00:00", End = "14:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Mittag Mo-Fr", Abbr = "MI-MF", Start = "10:00:00", End = "18:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Mittag Sa-So", Abbr = "MI-SS", Start = "10:00:00", End = "18:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Abend Mo-Fr", Abbr = "AB-MF", Start = "14:00:00", End = "22:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Abend Sa-So", Abbr = "AB-SS", Start = "14:00:00", End = "22:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Nacht Mo-Fr", Abbr = "NA-MF", Start = "22:00:00", End = "06:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Nacht Sa-So", Abbr = "NA-SS", Start = "22:00:00", End = "06:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Ganztag Mo-Fr", Abbr = "GZ-MF", Start = "00:00:00", End = "00:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Ganztag Sa-So", Abbr = "GZ-SS", Start = "00:00:00", End = "00:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Früh Mo-Fr", Abbr = "FR-MF", Start = "07:00:00", End = "15:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Früh Sa-So", Abbr = "FR-SS", Start = "07:00:00", End = "15:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Spät Mo-Fr", Abbr = "SP-MF", Start = "15:00:00", End = "23:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Spät Sa-So", Abbr = "SP-SS", Start = "15:00:00", End = "23:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Vormittag täglich", Abbr = "VM-TG", Start = "08:00:00", End = "12:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "Nachmittag täglich", Abbr = "NM-TG", Start = "12:00:00", End = "17:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = true, Sun = true },
                new { Name = "Bürozeiten Mo-Fr", Abbr = "BZ-MF", Start = "08:00:00", End = "17:00:00", Mon = true, Tue = true, Wed = true, Thu = true, Fri = true, Sat = false, Sun = false },
                new { Name = "Wochenende", Abbr = "WE", Start = "00:00:00", End = "00:00:00", Mon = false, Tue = false, Wed = false, Thu = false, Fri = false, Sat = true, Sun = true },
                new { Name = "Montag-Mittwoch", Abbr = "MO-MI", Start = "08:00:00", End = "16:00:00", Mon = true, Tue = true, Wed = true, Thu = false, Fri = false, Sat = false, Sun = false },
                new { Name = "Donnerstag-Freitag", Abbr = "DO-FR", Start = "08:00:00", End = "16:00:00", Mon = false, Tue = false, Wed = false, Thu = true, Fri = true, Sat = false, Sun = false }
            };

            foreach (var container in containers)
            {
                var containerId = Guid.NewGuid();
                var assignedGroups = GetRandomRootGroups(random.Next(1, 4));

                var cuttingAfterMidnight = (container.Start == "22:00:00" || container.Start == "23:00:00");

                script.AppendLine($@"
-- Container: {container.Name}
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{containerId}', {(cuttingAfterMidnight ? "true" : "false")}, 'Container für {container.Name}', '00000000-0000-0000-0000-000000000000', '{container.Name}', NULL, NULL, 2,
    '00:00:00', '00:00:00', '{container.End}', '{baseDate:yyyy-MM-dd}', '{container.Start}', NULL,
    {(container.Fri ? "true" : "false")}, false, {(container.Mon ? "true" : "false")}, {(container.Sat ? "true" : "false")}, {(container.Sun ? "true" : "false")}, {(container.Thu ? "true" : "false")}, {(container.Tue ? "true" : "false")}, {(container.Wed ? "true" : "false")},
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 1, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(1):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{container.Abbr}', '00:00:00', NULL,
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

        public static (string script, List<Guid> containerIds) GenerateContainers()
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
                "Deutschweiz Zürich",
                "Deutschweiz Mitte",
                "Deutschweiz Ost"
            };

            var containerTypes = new[]
            {
                new { Name = "Tag", Start = "06:00:00", End = "18:00:00", CuttingAfterMidnight = false },
                new { Name = "Abend", Start = "14:00:00", End = "22:00:00", CuttingAfterMidnight = false },
                new { Name = "Nacht", Start = "22:00:00", End = "06:00:00", CuttingAfterMidnight = true }
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
                        var name = $"Container {containerType.Name} {globalCounter}";
                        var abbr = $"C{containerType.Name[0]}{globalCounter}";

                        script.AppendLine($@"
-- Container #{globalCounter}: {name} ({containerType.Start}-{containerType.End})
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{containerId}', {(containerType.CuttingAfterMidnight ? "true" : "false")}, 'Container {containerType.Name} für {rootGroup}',
    '00000000-0000-0000-0000-000000000000', '{name}', NULL, NULL, 2,
    '00:00:00', '00:00:00', '{containerType.End}', '{baseDate:yyyy-MM-dd}', '{containerType.Start}', NULL,
    true, false, true, false, false, true, true, true,
    false, false, false, 1, '00:00:00', '00:00:00',
    8, 1, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(1):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{abbr}', '00:00:00', NULL,
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

        public static (string script, List<Guid> shiftIds) GenerateTimeRangeShiftsWithClients()
        {
            StringBuilder script = new StringBuilder();
            var shiftIds = new List<Guid>();
            var random = Random.Shared;
            var currentTime = DateTime.Now;
            var baseDate = new DateOnly(2025, 1, 1);

            script.AppendLine("\n-- TimeRange Shifts with Clients (100 Shifts PRO RootGroup = 400 total, 10-30 min WorkTime, 6-8h TimeRange)");
            script.AppendLine("-- WICHTIG: is_time_range=true, client_id wird per Subquery aus group_item geholt");
            script.AppendLine("-- Workflow: OriginalOrder (Status 0) -> SealedOrder (Status 1) -> OriginalShift (Status 2)");

            var availableRootGroups = new[] {
                "Westschweiz",
                "Deutschweiz Zürich",
                "Deutschweiz Mitte",
                "Deutschweiz Ost"
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
                    bool cuttingAfterMidnight;

                    if (crossesMidnight)
                    {
                        // Mitternachtsüberschreitung: Start zwischen 18:00 und 23:00
                        startHour = random.Next(18, 24);
                        endHour = (startHour + timeRangeHours) % 24;
                        // Wenn endHour kleiner als startHour ist, dann geht es über Mitternacht
                        cuttingAfterMidnight = endHour < startHour;
                    }
                    else
                    {
                        // Normale Shifts: Start zwischen 06:00 und (19 - timeRangeHours)
                        startHour = random.Next(6, Math.Max(7, 19 - timeRangeHours));
                        endHour = startHour + timeRangeHours;
                        cuttingAfterMidnight = false;
                    }

                    var shiftNumber = (groupIndex * shiftsPerGroup) + i;
                    var name = $"TimeRange-Shift {shiftNumber}";
                    var abbr = $"TR{shiftNumber}";

                    script.AppendLine($@"
-- TimeRange Shift #{shiftNumber} (WorkTime: {workTimeMinutes} min = {workTimeDecimal} h, Range: {timeRangeHours}h, {(cuttingAfterMidnight ? "Mitternacht!" : "Tagsüber")})
-- Step 1: Create OriginalOrder (Status = 0) with client_id from group_item
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{orderId}', {(cuttingAfterMidnight ? "true" : "false")}, 'TimeRange Shift {workTimeMinutes} Minuten Arbeitszeit in {timeRangeHours}h Zeitfenster{(cuttingAfterMidnight ? " (über Mitternacht)" : "")}',
    '00000000-0000-0000-0000-000000000000', '{name}', NULL, NULL, 0,
    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
    true, false, true, false, false, true, true, true,
    false, false, true, 1, '00:00:00', '00:00:00',
    {workTimeDecimal.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
    NULL, false, '{currentTime.AddMinutes(1):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{abbr}', '00:00:00',
    (SELECT gi.client_id FROM public.group_item gi
     JOIN public.""group"" g ON gi.group_id = g.id
     WHERE g.name = '{rootGroup}' AND gi.client_id IS NOT NULL AND gi.is_deleted = false
     ORDER BY random() LIMIT 1),
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
-- Step 3: Create OriginalShift (Status = 2) - 1:1 Kopie mit GLEICHEN Groups und client_id!
INSERT INTO public.shift (
    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
    debriefing_time, sum_employees, sporadic_scope, lft, rgt
) VALUES (
    '{originalShiftId}', {(cuttingAfterMidnight ? "true" : "false")}, 'TimeRange Shift {workTimeMinutes} Minuten Arbeitszeit in {timeRangeHours}h Zeitfenster{(cuttingAfterMidnight ? " (über Mitternacht)" : "")}',
    '00000000-0000-0000-0000-000000000000', '{name}', NULL, NULL, 2,
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
