using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    public partial class Add_AppUserId_To_GroupVisibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alte (falsche) Spalte löschen, falls vorhanden
            migrationBuilder.DropColumn(
                name: "AppUserId", // Wichtig: nur wenn schon in DB
                table: "group_visibility");

            // Neue Spalte hinzufügen (korrekter Snake Case)
            migrationBuilder.AddColumn<string>(
                name: "app_user_id",
                table: "group_visibility",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Index auf neue Spalte
            migrationBuilder.CreateIndex(
                name: "ix_group_visibility_app_user_id",
                table: "group_visibility",
                column: "app_user_id");

            // FK setzen
            migrationBuilder.AddForeignKey(
                name: "fk_group_visibility_app_user_app_user_id",
                table: "group_visibility",
                column: "app_user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FK löschen
            migrationBuilder.DropForeignKey(
                name: "fk_group_visibility_app_user_app_user_id",
                table: "group_visibility");

            // Index löschen
            migrationBuilder.DropIndex(
                name: "ix_group_visibility_app_user_id",
                table: "group_visibility");

            // Neue Spalte löschen
            migrationBuilder.DropColumn(
                name: "app_user_id",
                table: "group_visibility");

            // (Optional) alte Spalte wiederherstellen
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "group_visibility",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
