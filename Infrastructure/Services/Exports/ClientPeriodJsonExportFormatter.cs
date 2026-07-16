// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports client period data (hours per employee and external employee for a date range) as JSON,
/// grouped by client.
/// </summary>
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ClientPeriodJsonExportFormatter : IClientPeriodExportFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = ExportConstants.LineEnding
    };

    public string FormatKey => ExportConstants.FormatJson;

    public string ContentType => ExportConstants.ContentTypeJson;

    public string FileExtension => ".json";

    public byte[] Format(ClientPeriodExportData data, ExportOptions options)
    {
        var json = JsonSerializer.Serialize(data, SerializerOptions);
        return Encoding.UTF8.GetBytes(json);
    }
}
