using Microsoft.EntityFrameworkCore.Migrations;
using System.Text;

namespace Klacks.Api.Data.Seed
{
    public static class ContractsSeed
    {
        public static void SeedContracts(MigrationBuilder migrationBuilder)
        {
            var script = GenerateInsertScriptForContracts();
            migrationBuilder.Sql(script);
        }

        public static string GenerateInsertScriptForContracts()
        {
            StringBuilder script = new StringBuilder();
            var currentTime = DateTime.Now;
            var userId = "Anonymus";

            script.AppendLine("\n-- Contract entries");

            var contracts = new[]
            {
                // Bern (BE)
                new { Name = "Vollzeit 160 BE", Guaranteed = 180m, Max = 200m, Min = 160m, Canton = "BE" },
                new { Name = "Vollzeit 180 BE", Guaranteed = 160m, Max = 200m, Min = 120m, Canton = "BE" },
                new { Name = "Teilzeit 0 Std BE", Guaranteed = 0m, Max = 75m, Min = 0m, Canton = "BE" },
                
                // Zürich (ZH)
                new { Name = "Vollzeit 160 ZH", Guaranteed = 180m, Max = 200m, Min = 160m, Canton = "ZH" },
                new { Name = "Vollzeit 180 ZH", Guaranteed = 160m, Max = 200m, Min = 120m, Canton = "ZH" },
                new { Name = "Teilzeit 0 Std ZH", Guaranteed = 0m, Max = 75m, Min = 0m, Canton = "ZH" },
                
                // St. Gallen (SG)
                new { Name = "Vollzeit 160 SG", Guaranteed = 180m, Max = 200m, Min = 160m, Canton = "SG" },
                new { Name = "Vollzeit 180 SG", Guaranteed = 160m, Max = 200m, Min = 120m, Canton = "SG" },
                new { Name = "Teilzeit 0 Std SG", Guaranteed = 0m, Max = 75m, Min = 0m, Canton = "SG" },
                
                // Waadt/Lausanne (VD)
                new { Name = "Vollzeit 160 VD", Guaranteed = 180m, Max = 200m, Min = 160m, Canton = "VD" },
                new { Name = "Vollzeit 180 VD", Guaranteed = 160m, Max = 200m, Min = 120m, Canton = "VD" },
                new { Name = "Teilzeit 0 Std VD", Guaranteed = 0m, Max = 75m, Min = 0m, Canton = "VD" }
            };

            var validFrom = new DateTime(2025, 1, 1);

            foreach (var contract in contracts)
            {
                var contractId = Guid.NewGuid();
                var calendarSelectionId = SwissCantonCalendarSelectionsSeed.CalendarSelectionIds[contract.Canton];

                script.AppendLine($@"INSERT INTO public.contract (
                    id, name, guaranteed_hours_per_month, maximum_hours_per_month, minimum_hours_per_month,
                    valid_from, valid_until, calendar_selection_id,
                    create_time, update_time, is_deleted, current_user_created, current_user_updated
                ) VALUES (
                    '{contractId}', '{contract.Name}', {contract.Guaranteed}, {contract.Max}, {contract.Min},
                    '{validFrom:yyyy-MM-dd HH:mm:ss}', NULL, '{calendarSelectionId}',
                    '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', false, '{userId}', '{userId}'
                );");
            }

            return script.ToString();
        }
    }
}