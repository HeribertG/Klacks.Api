// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seed data for default report templates, providing an Absence Report template on fresh databases.
/// </summary>
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class ReportTemplatesSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"INSERT INTO public.report_templates (
                    id, name, description, type, source_id, data_set_ids, page_setup, sections,
                    merge_rows, show_full_period, create_time, current_user_created, current_user_updated,
                    update_time, deleted_time, is_deleted, current_user_deleted
                ) VALUES (
                    'e3b07a2c-5f9d-4e12-8b6a-1c3d5e7f9021',
                    'Absence Report',
                    '',
                    3,
                    'absence-gantt',
                    '[""absences""]',
                    '{""Size"": 0, ""Margins"": {""Top"": 20, ""Left"": 20, ""Right"": 20, ""Bottom"": 20}, ""Orientation"": 1}',
                    '[{""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 0, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Name"", ""Type"": 0, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 14, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""client.name"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Vorname"", ""Type"": 0, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 14, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""client.firstName"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Report-Datum"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""report.date"", ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 0, ""FreeTextRows"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 1, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Von"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 15, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""absence.from"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Bis"", ""Type"": 1, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 15, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 1, ""DataBinding"": ""absence.until"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Abwesenheit"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 2, ""DataBinding"": ""absence.absenceName"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Wert"", ""Type"": 2, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 1, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 10, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 3, ""DataBinding"": ""absence.value"", ""BindingSeparator"": null, ""AdditionalBindings"": null}, {""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Information"", ""Type"": 0, ""Style"": {""Bold"": false, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 30, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 4, ""DataBinding"": ""absence.information"", ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 1, ""FreeTextRows"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}, {""Id"": ""00000000-0000-0000-0000-000000000000"", ""Type"": 3, ""Fields"": [{""X"": 0, ""Y"": 0, ""Id"": ""00000000-0000-0000-0000-000000000000"", ""Name"": ""Anzahl"", ""Type"": 2, ""Style"": {""Bold"": true, ""Italic"": false, ""FontSize"": 10, ""Alignment"": 0, ""TextColor"": ""#000000"", ""Underline"": false, ""CellBorder"": null, ""FontFamily"": ""helvetica"", ""BackgroundColor"": ""#FFFFFF""}, ""Width"": 25, ""Format"": null, ""Height"": 0, ""Formula"": null, ""ImageUrl"": null, ""HideLabel"": false, ""SortOrder"": 0, ""DataBinding"": ""absence.totalCount"", ""BindingSeparator"": null, ""AdditionalBindings"": null}], ""Height"": 0, ""Visible"": true, ""SortOrder"": 2, ""FreeTextRows"": null, ""BackgroundColor"": null, ""TableFooterFields"": null}]',
                    false,
                    false,
                    '2026-01-01 00:00:00.000+00',
                    'admin',
                    'admin',
                    '2026-01-01 00:00:00.000+00',
                    NULL,
                    false,
                    ''
                ) ON CONFLICT (id) DO NOTHING;"
            );
        }
    }
}
