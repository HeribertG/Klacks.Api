using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeEscalationChainForProactiveApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_escalation_chains_work_id",
                table: "escalation_chains");

            migrationBuilder.AlterColumn<Guid>(
                name: "work_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "shift_start_utc",
                table: "escalation_chains",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "absent_client_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "condition_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "purpose",
                table: "escalation_chains",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_condition_id",
                table: "escalation_chains",
                column: "condition_id",
                unique: true,
                filter: "\"condition_id\" IS NOT NULL AND \"is_deleted\" = false AND \"status\" = 0");

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_work_id",
                table: "escalation_chains",
                column: "work_id",
                unique: true,
                filter: "\"work_id\" IS NOT NULL AND \"is_deleted\" = false AND \"status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_escalation_chains_condition_id",
                table: "escalation_chains");

            migrationBuilder.DropIndex(
                name: "ix_escalation_chains_work_id",
                table: "escalation_chains");

            migrationBuilder.DropColumn(
                name: "condition_id",
                table: "escalation_chains");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "escalation_chains");

            migrationBuilder.AlterColumn<Guid>(
                name: "work_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "shift_start_utc",
                table: "escalation_chains",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "absent_client_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_work_id",
                table: "escalation_chains",
                column: "work_id",
                unique: true,
                filter: "\"is_deleted\" = false AND \"status\" = 0");
        }
    }
}
