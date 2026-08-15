using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiInstanceUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every get-or-create path below reads a row and inserts one when it finds none, so an
            // installation that ever ran two API instances can already hold duplicates. The index
            // build would fail on them; these cleanups run first and are idempotent. The settings
            // table has no soft-delete column, so its surplus rows are removed outright.
            migrationBuilder.Sql(@"
                DELETE FROM public.settings a
                USING public.settings b
                WHERE a.type = b.type AND a.id > b.id;");

            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT id,
                           ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY create_time ASC, id ASC) AS rn
                    FROM public.heartbeat_configs
                    WHERE is_deleted = false
                )
                UPDATE public.heartbeat_configs h
                SET is_deleted = true, deleted_time = NOW()
                FROM ranked r
                WHERE h.id = r.id AND r.rn > 1;");

            migrationBuilder.CreateIndex(
                name: "ix_settings_type",
                table: "settings",
                column: "type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_heartbeat_configs_user_id",
                table: "heartbeat_configs",
                column: "user_id",
                unique: true,
                filter: "\"is_deleted\" = false");

            // The model sync compares ApiModelId with OrdinalIgnoreCase against the rows of one
            // ProviderId, so the constraint that matches the code is (provider_id, lower(api_model_id)):
            // a plain index over api_model_id would treat "Claude" and "claude" as different keys and
            // let through exactly the duplicate this guards against. An expression index cannot be
            // declared through the fluent API, so it stays raw SQL and out of the model snapshot.
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT id,
                           ROW_NUMBER() OVER (PARTITION BY provider_id, lower(api_model_id) ORDER BY create_time ASC, id ASC) AS rn
                    FROM public.llm_models
                    WHERE is_deleted = false
                )
                UPDATE public.llm_models m
                SET is_deleted = true, deleted_time = NOW()
                FROM ranked r
                WHERE m.id = r.id AND r.rn > 1;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_llm_models_provider_id_api_model_id_lower
                ON public.llm_models (provider_id, lower(api_model_id))
                WHERE is_deleted = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_settings_type",
                table: "settings");

            migrationBuilder.DropIndex(
                name: "ix_heartbeat_configs_user_id",
                table: "heartbeat_configs");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_llm_models_provider_id_api_model_id_lower;");
        }
    }
}
