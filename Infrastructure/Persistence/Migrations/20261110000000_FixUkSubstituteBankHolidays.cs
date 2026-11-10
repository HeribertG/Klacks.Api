using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Corrects the weekend substitution of the seeded UK bank holidays (England and Wales). The seed shipped
    /// 'SA-1;SU+1', which moved a Saturday holiday back to Friday; in the UK the substitute is always the next free
    /// weekday. New Year's Day becomes 'SA+2;SU+1', Christmas Day and Boxing Day become 'SA+2;SU+2'.
    /// Only rows that still carry the shipped value are changed, so a rule a customer edited stays as it is.
    /// A fresh database has no such rows yet when this runs; the corrected seed inserts them afterwards.
    /// The literals are frozen on purpose: a migration must keep meaning what it meant when it was written.
    /// Down restores the shipped value on rows that still carry the corrected one.
    /// </summary>
    public partial class FixUkSubstituteBankHolidays : Migration
    {
        public const string UpSql = """
            UPDATE calendar_rule SET sub_rule = 'SA+2;SU+1'
            WHERE id = '06a00001-0001-0001-0001-000000000001' AND sub_rule = 'SA-1;SU+1';
            UPDATE calendar_rule SET sub_rule = 'SA+2;SU+2'
            WHERE id IN ('06a00001-0001-0001-0001-000000000007', '06a00001-0001-0001-0001-000000000008')
              AND sub_rule = 'SA-1;SU+1';
            """;

        public const string DownSql = """
            UPDATE calendar_rule SET sub_rule = 'SA-1;SU+1'
            WHERE id = '06a00001-0001-0001-0001-000000000001' AND sub_rule = 'SA+2;SU+1';
            UPDATE calendar_rule SET sub_rule = 'SA-1;SU+1'
            WHERE id IN ('06a00001-0001-0001-0001-000000000007', '06a00001-0001-0001-0001-000000000008')
              AND sub_rule = 'SA+2;SU+2';
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DownSql);
        }
    }
}
