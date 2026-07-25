using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractIndividualPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "individual_period_id",
                table: "contract",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_individual_period_id",
                table: "contract",
                column: "individual_period_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_individual_period_individual_period_id",
                table: "contract",
                column: "individual_period_id",
                principalTable: "individual_period",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contract_individual_period_individual_period_id",
                table: "contract");

            migrationBuilder.DropIndex(
                name: "ix_contract_individual_period_id",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "individual_period_id",
                table: "contract");
        }
    }
}
