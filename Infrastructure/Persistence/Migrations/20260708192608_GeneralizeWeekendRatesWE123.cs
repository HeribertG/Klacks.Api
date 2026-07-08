using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeWeekendRatesWE123 : Migration
    {
        // Country-neutral weekend surcharge rates: the calendar-bound sa_rate/so_rate become generic
        // weekend-slot rates we1rate/we2rate (preserving existing values: slot 1 = former Saturday,
        // slot 2 = former Sunday) and a third slot we3rate is added for calendars with three weekend days.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "sa_rate", table: "contract", newName: "we1rate");
            migrationBuilder.RenameColumn(name: "so_rate", table: "contract", newName: "we2rate");
            migrationBuilder.AddColumn<decimal>(name: "we3rate", table: "contract", type: "numeric", nullable: true);

            migrationBuilder.RenameColumn(name: "sa_rate", table: "scheduling_rules", newName: "we1rate");
            migrationBuilder.RenameColumn(name: "so_rate", table: "scheduling_rules", newName: "we2rate");
            migrationBuilder.AddColumn<decimal>(name: "we3rate", table: "scheduling_rules", type: "numeric", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "we3rate", table: "contract");
            migrationBuilder.RenameColumn(name: "we2rate", table: "contract", newName: "so_rate");
            migrationBuilder.RenameColumn(name: "we1rate", table: "contract", newName: "sa_rate");

            migrationBuilder.DropColumn(name: "we3rate", table: "scheduling_rules");
            migrationBuilder.RenameColumn(name: "we2rate", table: "scheduling_rules", newName: "so_rate");
            migrationBuilder.RenameColumn(name: "we1rate", table: "scheduling_rules", newName: "sa_rate");
        }
    }
}
