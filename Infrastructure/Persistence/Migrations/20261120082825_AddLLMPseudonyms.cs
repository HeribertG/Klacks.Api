using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMPseudonyms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "llm_pseudonyms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llm_pseudonyms", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_llm_pseudonyms_kind_key",
                table: "llm_pseudonyms",
                columns: new[] { "kind", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_llm_pseudonyms_kind_ordinal",
                table: "llm_pseudonyms",
                columns: new[] { "kind", "ordinal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "llm_pseudonyms");
        }
    }
}
