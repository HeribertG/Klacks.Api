// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of the SQL that installs the default group detail report template.
/// Consumed by the fresh-install seed and by the migration that adds it to existing databases.
/// </summary>
/// <remarks>
/// The template is inserted with ON CONFLICT DO NOTHING so administrator changes are never overwritten,
/// and the REPORT_DEFAULT_TEMPLATES setting is merged so existing source assignments win.
/// DataBinding keys must match the Angular report catalog (report-data-source.model.ts) and the
/// resolvers in report-data-provider.service.ts exactly, or the printed PDF renders empty columns.
/// </remarks>
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class EditGroupReportTemplatesSql
    {
        public const string GroupDetailTemplateId = "019e0a00-0009-7000-8000-000000000009";

        private const string DefaultTemplatesSettingsId = "019dd2f1-d67d-750b-96a4-b530e5b30457";

        private const string DefaultTemplatesSettingsType = "REPORT_DEFAULT_TEMPLATES";

        private const string SeedTimestamp = "2026-01-01 00:00:00.000+00";

        private const string SeedUser = "admin";

        private const string PageSetup = @"{""Size"": 0, ""Margins"": {""Top"": 20, ""Left"": 20, ""Right"": 20, ""Bottom"": 20}, ""Orientation"": 0}";

        private const string NewDefaultTemplates = $@"{{""edit-group"": ""{GroupDetailTemplateId}""}}";

        private const string GroupDetailSections = @"[{""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 0, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Group Detail Report"", ""Type"": 0, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""report.customText"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Group Name"", ""Type"": 0, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""group.name"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Group Path"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 60, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 2, ""DataBinding"": ""group.path"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Valid From"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 3, ""DataBinding"": ""group.validFrom"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Valid Until"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 4, ""DataBinding"": ""group.validUntil"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Payment Interval"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 5, ""DataBinding"": ""group.paymentInterval"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Calendar"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 6, ""DataBinding"": ""group.calendarName"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Clients"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 15, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 7, ""DataBinding"": ""group.clientsCount"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Shifts"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 15, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 8, ""DataBinding"": ""group.shiftsCount"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Description"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 60, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 9, ""DataBinding"": ""group.description"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Composition"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 60, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 10, ""DataBinding"": ""group.membersSummary"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Sub Groups"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 60, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 11, ""DataBinding"": ""group.subGroupsSummary"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 0, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 1, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Number"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 12, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""groupMember.idNumber"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Company"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""groupMember.company"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""First Name"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 20, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 2, ""DataBinding"": ""groupMember.firstName"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Name"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 20, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 3, ""DataBinding"": ""groupMember.name"", ""SortDirection"": ""asc"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Valid From"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 1, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 11, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 4, ""DataBinding"": ""groupMember.validFrom"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Valid Until"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 1, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 12, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 5, ""DataBinding"": ""groupMember.validUntil"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 1, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Total Members"", ""Type"": 2, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""groupMember.totalCount"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}]}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 3, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Page"", ""Type"": 9, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""report.pageNumber"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 2, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}]";

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(InsertTemplate(GroupDetailTemplateId, "Group Detail Report", 0, "edit-group", @"[""details""]", GroupDetailSections));

            migrationBuilder.Sql(InsertDefaultTemplatesSetting());
            migrationBuilder.Sql(MergeDefaultTemplatesSetting());
        }

        public static void Remove(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $@"DELETE FROM public.report_templates
                  WHERE id = '{GroupDetailTemplateId}';");
        }

        private static string InsertTemplate(string id, string name, int type, string sourceId, string dataSetIds, string sections)
        {
            return $@"INSERT INTO public.report_templates (
                        id, name, description, type, source_id, data_set_ids, page_setup, sections,
                        parameters, versions, merge_rows, show_full_period, create_time, current_user_created,
                        current_user_updated, update_time, deleted_time, is_deleted, current_user_deleted
                      ) VALUES (
                        '{id}',
                        '{name}',
                        '',
                        {type},
                        '{sourceId}',
                        '{dataSetIds}',
                        '{PageSetup}',
                        '{sections}',
                        '[]',
                        '[]',
                        false,
                        false,
                        '{SeedTimestamp}',
                        '{SeedUser}',
                        '{SeedUser}',
                        '{SeedTimestamp}',
                        NULL,
                        false,
                        ''
                      ) ON CONFLICT (id) DO NOTHING;";
        }

        private static string InsertDefaultTemplatesSetting()
        {
            return $@"INSERT INTO public.settings (id, type, value)
                      SELECT '{DefaultTemplatesSettingsId}', '{DefaultTemplatesSettingsType}', '{NewDefaultTemplates}'
                      WHERE NOT EXISTS (
                        SELECT 1 FROM public.settings WHERE type = '{DefaultTemplatesSettingsType}'
                      );";
        }

        private static string MergeDefaultTemplatesSetting()
        {
            return $@"UPDATE public.settings
                      SET value = ('{NewDefaultTemplates}'::jsonb || value::jsonb)::text
                      WHERE type = '{DefaultTemplatesSettingsType}';";
        }
    }
}
