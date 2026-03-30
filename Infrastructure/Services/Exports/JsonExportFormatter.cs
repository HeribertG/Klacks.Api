// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as structured JSON for API integration and ERP import.
/// Preserves the hierarchical structure: Orders -> WorkEntries -> Changes/Expenses/Breaks.
/// </summary>
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class JsonExportFormatter : IExportFormatter
{
    public string FormatKey => ExportConstants.FormatJson;

    public string ContentType => ExportConstants.ContentTypeJson;

    public string FileExtension => ".json";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        return JsonSerializer.SerializeToUtf8Bytes(data, jsonOptions);
    }
}
