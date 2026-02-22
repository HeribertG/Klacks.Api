// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed;

public static class AiGuidelinesSeed
{
    public static void SeedData(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        var guidelinesContent = @"- Be polite and professional
- Use available functions when users ask for them
- Give clear and precise instructions
- Always check permissions before executing functions
- For missing permissions: explain that the user needs to contact an administrator";

        var escapedContent = guidelinesContent.Replace("'", "''");

        migrationBuilder.Sql($@"
            INSERT INTO ai_guidelines (id, name, content, is_active, source, is_deleted, create_time)
            SELECT gen_random_uuid(), 'Default Guidelines', '{escapedContent}', true, 'seed', false, '{now:yyyy-MM-dd HH:mm:ss}'
            WHERE NOT EXISTS (SELECT 1 FROM ai_guidelines WHERE is_deleted = false);
        ");
    }
}
