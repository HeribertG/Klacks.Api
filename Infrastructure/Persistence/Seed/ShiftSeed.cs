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

            // Available canton group names for random assignment
            var availableCantons = new[] { "ZH", "BE", "LU", "SG", "AG", "BS", "BL", "GE", "VD", "NE", "JU", "FR" };

            List<string> GetRandomCantons(int count)
            {
                return availableCantons.OrderBy(x => random.Next()).Take(count).ToList();
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

            // 1. Create 5 simple OriginalOrder shifts (Status = 0)
            var simpleShifts = new[]
            {
                new { Name = "Frühschicht", Abbr = "FS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Spätschicht", Abbr = "SS", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, CuttingAfterMidnight = false },
                new { Name = "Nachtschicht", Abbr = "NS", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = true },
                new { Name = "Tagdienst", Abbr = "TAG", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Bereitschaft", Abbr = "BD", Start = "00:00:00", End = "24:00:00", WorkTime = 24, Employees = 1, CuttingAfterMidnight = false }
            };

            script.AppendLine("\n-- 1. Simple OriginalOrder Shifts (Status = 0)");
            foreach (var shift in simpleShifts)
            {
                var shiftId = Guid.NewGuid();
                var uniqueName = GetUniqueName(shift.Name, 1);
                var uniqueAbbr = GetUniqueAbbreviation(shift.Abbr, 1);
                var assignedGroups = GetRandomCantons(random.Next(1, 3)); // 1-2 random groups

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{shiftId}', {(shift.CuttingAfterMidnight ? "true" : "false")}, '{shift.Name} mit {shift.Employees} Mitarbeiter(n)', '00000000-0000-0000-0000-000000000000', '{uniqueName}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{shift.End}', '{baseDate:yyyy-MM-dd}', '{shift.Start}', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    {shift.WorkTime}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbr}', '00:00:00', NULL,
                    '00:00:00', {shift.Employees}, 0, NULL, NULL
                );");

                shiftIds.Add(shiftId);
                TrackShiftGroups(shiftId, assignedGroups);
            }

            // 2. Create 20 Morgenschichten (6 Stunden) - OriginalOrder (Status = 0)
            script.AppendLine("\n-- 2. Morning Shifts - OriginalOrder (Status = 0)");
            for (int i = 1; i <= 20; i++)
            {
                var morningShiftId = Guid.NewGuid();
                var startHour = random.Next(5, 8);
                var endHour = startHour + 6;
                var employees = (i <= 3) ? 2 : 1;
                var uniqueNameMorning = GetUniqueName("Morgenschicht", i);
                var uniqueAbbrMorning = GetUniqueAbbreviation("MOR", i);
                var assignedGroups = GetRandomCantons(random.Next(1, 3)); // 1-2 random groups

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{morningShiftId}', false, '6-Stunden Morgenschicht - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameMorning}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    6, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(10):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrMorning}', '00:00:00', NULL,
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(morningShiftId);
                TrackShiftGroups(morningShiftId, assignedGroups);
            }

            // 3. Create 30 Tagschichten Mo-Fr 08:00-17:00 - OriginalOrder (Status = 0)
            script.AppendLine("\n-- 3. Day Shifts Mo-Fr - OriginalOrder (Status = 0)");
            for (int i = 1; i <= 30; i++)
            {
                var dayShiftId = Guid.NewGuid();
                var employees = (i <= 5) ? 2 : 1;
                var uniqueNameDay = GetUniqueName("Tagschicht", i);
                var uniqueAbbrDay = GetUniqueAbbreviation("TAG", i + 100);
                var assignedGroups = GetRandomCantons(random.Next(1, 3)); // 1-2 random groups

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{dayShiftId}', false, 'Tagschicht Mo-Fr mit 1h Mittagspause - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameDay}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '17:00:00', '{baseDate:yyyy-MM-dd}', '08:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(15):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrDay}', '00:00:00', NULL,
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(dayShiftId);
                TrackShiftGroups(dayShiftId, assignedGroups);
            }

            // 4. Create 20 Nachtdienste Mo-Fr 23:00-07:00 - OriginalOrder (Status = 0)
            script.AppendLine("\n-- 4. Night Shifts Mo-Fr - OriginalOrder (Status = 0)");
            for (int i = 1; i <= 20; i++)
            {
                var nightShiftId = Guid.NewGuid();
                var uniqueNameNightMF = GetUniqueName("Nachtdienst Mo-Fr", i);
                var uniqueAbbrNightMF = GetUniqueAbbreviation("NMF", i);
                var assignedGroups = GetRandomCantons(random.Next(1, 3)); // 1-2 random groups

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{nightShiftId}', true, 'Nachtdienst Mo-Fr - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightMF}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, false, true, false, false, true, true, false,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(20):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightMF}', '00:00:00', NULL,
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(nightShiftId);
                TrackShiftGroups(nightShiftId, assignedGroups);
            }

            // 5. Create 20 Nachtdienste Sa-So 23:00-07:00 - OriginalOrder (Status = 0)
            script.AppendLine("\n-- 5. Night Shifts Sa-So - OriginalOrder (Status = 0)");
            for (int i = 1; i <= 20; i++)
            {
                var weekendNightId = Guid.NewGuid();
                var uniqueNameNightSS = GetUniqueName("Nachtdienst Sa-So", i);
                var uniqueAbbrNightSS = GetUniqueAbbreviation("NSS", i);
                var assignedGroups = GetRandomCantons(random.Next(1, 3)); // 1-2 random groups

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{weekendNightId}', true, 'Nachtdienst Sa-So - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightSS}', NULL, NULL, 0,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    false, false, false, true, true, false, false, false,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{user}', NULL, '{user}',
                    NULL, false, '{currentTime.AddMinutes(25):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightSS}', '00:00:00', NULL,
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(weekendNightId);
                TrackShiftGroups(weekendNightId, assignedGroups);
            }

            // 6. Create 10 sealed shifts with splits to demonstrate the complete workflow
            script.AppendLine("\n-- 6. Example Sealed Shifts with Splits (Status 0 -> 1 -> 3 Children) - SEEDING VERSION");
            script.AppendLine("-- WICHTIG: Beim Seeding werden KEINE ROOT SplitShifts erstellt!");
            script.AppendLine("-- Stattdessen: 3 eigenständige SplitShifts als Geschwister (parent_id=NULL, root_id=SealedOrder, lft=1, rgt=2)");
            for (int i = 1; i <= 10; i++)
            {
                var orderId = Guid.NewGuid(); // EINE ID für Order (wird von Status 0 -> 1 updated)
                var employees = (i <= 2) ? 2 : 1;

                var uniqueName24h = GetUniqueName("24h-Schichtdienst", i);
                var uniqueAbbr24h = GetUniqueAbbreviation("24H", i);

                // WICHTIG: ALLE Shifts in diesem Workflow bekommen die GLEICHEN Groups!
                var workflowGroups = GetRandomCantons(random.Next(1, 2)); // 1 group for workflow consistency

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
    }
}
