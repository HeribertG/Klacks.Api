using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Retypes the 148 curated "see also" edges from CoRequired to the new, retrieval-neutral SeeAlso
    /// type. The seed loader is insert-only and keyed on (skill A, skill B, type), so a database that
    /// already carries these edges would keep the CoRequired rows forever and SkillRetrievalExpander
    /// would keep spending its three expansion slots on documentation cross-references at confidence
    /// 1.0 — above anything the learner can ever reach (0.95). Keyed on the provenance the loader
    /// writes, so learned co-occurrence edges are untouched.
    /// </summary>
    public partial class RetypeSeededSeeAlsoRelations : Migration
    {
        private const string SeeAlsoProvenance = "seed:curated:seealso";
        private const int CoRequiredType = 0;
        private const int SeeAlsoType = 4;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE skill_relations SET type = {SeeAlsoType} " +
                $"WHERE provenance = '{SeeAlsoProvenance}' AND type = {CoRequiredType};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE skill_relations SET type = {CoRequiredType} " +
                $"WHERE provenance = '{SeeAlsoProvenance}' AND type = {SeeAlsoType};");
        }
    }
}
