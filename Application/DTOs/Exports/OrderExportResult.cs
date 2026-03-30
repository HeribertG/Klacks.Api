// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of an order export operation containing the file content.
/// @param FileContent - The generated file as byte array
/// @param FileName - Suggested file name with extension
/// @param ContentType - MIME type for the HTTP response
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class OrderExportResult
{
    public byte[] FileContent { get; set; } = [];

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
}
