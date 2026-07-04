// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Strategy interface for client period export format implementations.
/// </summary>
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Domain.Interfaces.Exports;

public interface IClientPeriodExportFormatter
{
    string ContentType { get; }

    string FileExtension { get; }

    byte[] Format(ClientPeriodExportData data, ExportOptions options);
}
