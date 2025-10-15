using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeClientAndGroupItemIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_client_is_deleted_name_first_name
                ON client (is_deleted, name, first_name);

                CREATE INDEX IF NOT EXISTS ix_client_is_deleted_first_name_name
                ON client (is_deleted, first_name, name);

                CREATE INDEX IF NOT EXISTS ix_client_is_deleted_company_name
                ON client (is_deleted, company, name);

                CREATE INDEX IF NOT EXISTS ix_client_is_deleted_id_number
                ON client (is_deleted, id_number);

                CREATE INDEX IF NOT EXISTS ix_group_item_group_id_client_id_is_deleted
                ON group_item (group_id, client_id, is_deleted);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_client_is_deleted_name_first_name;
                DROP INDEX IF EXISTS ix_client_is_deleted_first_name_name;
                DROP INDEX IF EXISTS ix_client_is_deleted_company_name;
                DROP INDEX IF EXISTS ix_client_is_deleted_id_number;
                DROP INDEX IF EXISTS ix_group_item_group_id_client_id_is_deleted;
            ");
        }
    }
}
