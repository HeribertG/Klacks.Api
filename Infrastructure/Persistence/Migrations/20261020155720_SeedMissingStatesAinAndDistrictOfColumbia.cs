using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the two state rows the country region files need but the seed lacked on existing databases: the
    /// French département 01 (Ain) and the US District of Columbia (DC, country prefix 'USA' like every other
    /// seeded US state). Same ids as DefaultSeed. A row is only inserted when the country's states are already
    /// seeded and the code is still missing: on a fresh database the migrations run before DefaultSeed, the
    /// state table is empty, nothing is inserted here and DefaultSeed (which has no ON CONFLICT on states)
    /// adds both rows itself. Data only - the model is unchanged.
    /// </summary>
    public partial class SeedMissingStatesAinAndDistrictOfColumbia : Migration
    {
        private const string AinId = "d6ae2a47-8e65-4ccd-bbe9-000000000001";
        private const string DistrictOfColumbiaId = "cf9361fb-5792-4d93-9ad5-000000000061";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(InsertWhenMissing(
                AinId, "01", "FR",
                @"{""de"":""Ain"",""en"":""Ain"",""fr"":""Ain"",""it"":""Ain""}"));

            migrationBuilder.Sql(InsertWhenMissing(
                DistrictOfColumbiaId, "DC", "USA",
                @"{""de"":""District of Columbia"",""en"":""District of Columbia"",""fr"":""District de Columbia"",""it"":""Distretto di Columbia""}"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM public.state WHERE id IN ('{AinId}', '{DistrictOfColumbiaId}');");
        }

        private static string InsertWhenMissing(string id, string abbreviation, string countryPrefix, string namesJson) =>
            "INSERT INTO public.state (id, abbreviation, country_prefix, name, create_time, current_user_created, " +
            "update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) " +
            $"SELECT '{id}', '{abbreviation}', '{countryPrefix}', '{namesJson}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL " +
            $"WHERE EXISTS (SELECT 1 FROM public.state WHERE country_prefix = '{countryPrefix}') " +
            $"AND NOT EXISTS (SELECT 1 FROM public.state WHERE country_prefix = '{countryPrefix}' AND abbreviation = '{abbreviation}') " +
            "ON CONFLICT (id) DO NOTHING;";
    }
}
