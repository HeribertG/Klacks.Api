using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Removes the leftovers of the retired language-model mode of wizard stage 3 (Holistic Harmonizer, owner decision
    /// 2026-11-11): the Klacksy skills get_wizard_settings and update_wizard_settings and the three stage-3 settings
    /// WIZARD3_MODE, WIZARD3_LLM_MODEL and WIZARD3_CRITIC_MODEL, which no code reads any more. skill-seeds.json no
    /// longer defines the skills, but the seed loader only upserts and never prunes, so the seeded catalog rows would
    /// survive without a handler. agent_skill_executions cascade with the skill row; skill_phrase, skill_relations and
    /// proposed_skill_changes address the skill by name without a foreign key and are deleted explicitly, before the
    /// skill rows. The literals are frozen on purpose: a migration must keep meaning what it meant when it was written.
    /// Down is empty: skill-seeds.json is the source of truth and no longer defines the skills, and the settings had
    /// no reader left.
    /// </summary>
    public partial class RemoveHarmonizerLlmMode : Migration
    {
        public const string DeleteSkillPhrasesSql = """
            DELETE FROM skill_phrase
            WHERE owner_kind = 'Skill' AND owner_name IN ('get_wizard_settings', 'update_wizard_settings');
            """;

        public const string DeleteSkillRelationsSql = """
            DELETE FROM skill_relations
            WHERE skill_a_name IN ('get_wizard_settings', 'update_wizard_settings')
               OR skill_b_name IN ('get_wizard_settings', 'update_wizard_settings');
            """;

        public const string DeleteProposedSkillChangesSql = """
            DELETE FROM proposed_skill_changes
            WHERE skill_name IN ('get_wizard_settings', 'update_wizard_settings');
            """;

        public const string DeleteSkillsSql = """
            DELETE FROM agent_skills
            WHERE name IN ('get_wizard_settings', 'update_wizard_settings');
            """;

        public const string DeleteSettingsSql = """
            DELETE FROM settings
            WHERE type IN ('WIZARD3_MODE', 'WIZARD3_LLM_MODEL', 'WIZARD3_CRITIC_MODEL');
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DeleteSkillPhrasesSql);
            migrationBuilder.Sql(DeleteSkillRelationsSql);
            migrationBuilder.Sql(DeleteProposedSkillChangesSql);
            migrationBuilder.Sql(DeleteSkillsSql);
            migrationBuilder.Sql(DeleteSettingsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
