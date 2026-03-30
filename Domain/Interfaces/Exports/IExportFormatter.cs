// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Strategy interface for export format implementations.
/// Each format (CSV, JSON, XML, DATEV, etc.) implements this interface.
/// </summary>
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Domain.Interfaces.Exports;

public interface IExportFormatter
{
    string FormatKey { get; }

    string ContentType { get; }

    string FileExtension { get; }

    byte[] Format(OrderExportData data, ExportOptions options);
}
