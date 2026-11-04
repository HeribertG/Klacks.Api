using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds work.split_from_work_id: the second and later parts of a split container name the first part of their split
    /// family, so the shift grid counts the family as one planned container. Existing works are no split parts (null).
    /// </summary>
    public partial class AddWorkSplitFromWorkId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "split_from_work_id",
                table: "work",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "split_from_work_id",
                table: "work");
        }
    }
}
