using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAddressDetailReportTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            QuickPrintReportTemplatesSql.ExtendAddressDetailWithClientDetails(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            QuickPrintReportTemplatesSql.RevertAddressDetailClientDetailsExtension(migrationBuilder);
        }
    }
}
