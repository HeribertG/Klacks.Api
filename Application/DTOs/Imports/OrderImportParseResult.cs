// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Imports;

public class OrderImportParseResult
{
    public int? SchemaVersion { get; set; }

    public List<ImportedOrderPayload> Orders { get; set; } = [];

    public List<OrderImportValidationError> Errors { get; set; } = [];

    public bool HasErrors => Errors.Count > 0;
}
