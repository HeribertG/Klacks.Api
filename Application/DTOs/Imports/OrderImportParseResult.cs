// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Imports;

public class OrderImportParseResult
{
    public int? SchemaVersion { get; set; }

    public List<ImportedOrderPayload> Orders { get; set; } = [];

    public List<OrderImportValidationError> Errors { get; set; } = [];

    public bool HasErrors => Errors.Count > 0;
}
