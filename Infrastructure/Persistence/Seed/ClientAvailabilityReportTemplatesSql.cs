// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of the SQL that installs the two default client-availability report templates
/// (client availability list, client availability detail).
/// Consumed by the fresh-install seed and by the migration that adds them to existing databases.
/// </summary>
/// <remarks>
/// Templates are inserted with ON CONFLICT DO NOTHING so administrator changes are never overwritten,
/// and the REPORT_DEFAULT_TEMPLATES setting is merged so existing source assignments win.
/// DataBinding keys must match the Angular report catalog (report-data-source.model.ts) and the
/// resolvers in report-data-provider.service.ts exactly, or the printed PDF renders empty columns.
/// </remarks>
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class ClientAvailabilityReportTemplatesSql
    {
        public const string ClientAvailabilityListTemplateId = "019e0a00-0007-7000-8000-000000000007";
        public const string ClientAvailabilityDetailTemplateId = "019e0a00-0008-7000-8000-000000000008";

        private const string DefaultTemplatesSettingsId = "019dd2f1-d67d-750b-96a4-b530e5b30457";

        private const string DefaultTemplatesSettingsType = "REPORT_DEFAULT_TEMPLATES";

        private const string SeedTimestamp = "2026-01-01 00:00:00.000+00";

        private const string SeedUser = "admin";

        private const string PageSetup = @"{""Size"": 0, ""Margins"": {""Top"": 20, ""Left"": 20, ""Right"": 20, ""Bottom"": 20}, ""Orientation"": 1}";

        private const string NewDefaultTemplates = $@"{{""client-availability"": ""{ClientAvailabilityListTemplateId}"", ""edit-client-availability"": ""{ClientAvailabilityDetailTemplateId}""}}";

        private const string ClientAvailabilityListSections = @"[{""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 0, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Client Availability List Report"", ""Type"": 0, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""report.customText"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Reporting Period"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""report.period"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 0, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 1, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""First Name"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 20, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""client.list.firstName"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Name"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 20, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""client.list.name"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Company"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 2, ""DataBinding"": ""client.list.company"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Total Hours"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 15, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 3, ""DataBinding"": ""availability.totalHours"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Days With Availability"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 20, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 4, ""DataBinding"": ""availability.daysWithAvailability"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 1, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Total Clients"", ""Type"": 2, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""client.totalCount"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}]}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 3, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Page"", ""Type"": 9, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""report.pageNumber"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 2, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}]";

        private const string ClientAvailabilityDetailSections = @"[{""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 0, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Client Availability Detail Report"", ""Type"": 0, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""report.customText"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Name"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""client.name"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""First Name"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 2, ""DataBinding"": ""client.firstName"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Company"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 3, ""DataBinding"": ""client.company"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Reporting Period"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 4, ""DataBinding"": ""report.period"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 0, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 1, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Date"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 1, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 15, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""availability.date"", ""SortDirection"": ""asc"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Weekday"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 12, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""availability.weekday"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Time Ranges"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 40, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 2, ""DataBinding"": ""availability.ranges"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Hours"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 10, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 3, ""DataBinding"": ""availability.hours"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 1, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 3, ""Title"": null, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Page"", ""Type"": 9, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 2, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 20, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""report.pageNumber"", ""SortDirection"": null, ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 2, ""FreeTextRows"": null, ""WidthPercent"": null, ""ShowFullPeriod"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}]";

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(InsertTemplate(ClientAvailabilityListTemplateId, "Client Availability List Report", 1, "client-availability", @"[""clients""]", ClientAvailabilityListSections));
            migrationBuilder.Sql(InsertTemplate(ClientAvailabilityDetailTemplateId, "Client Availability Detail Report", 1, "edit-client-availability", @"[""details""]", ClientAvailabilityDetailSections));

            migrationBuilder.Sql(InsertDefaultTemplatesSetting());
            migrationBuilder.Sql(MergeDefaultTemplatesSetting());
        }

        public static void Remove(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $@"DELETE FROM public.report_templates
                  WHERE id IN (
                    '{ClientAvailabilityListTemplateId}',
                    '{ClientAvailabilityDetailTemplateId}'
                  );");
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
