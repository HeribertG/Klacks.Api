// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportPreviewRow
{
    public int RowIndex { get; set; }

    public ClientImportRowStatus Status { get; set; }

    public ClientImportRecord Record { get; set; } = new();

    public List<ClientImportIssue> Issues { get; set; } = [];

    public Guid? DuplicateOfClientId { get; set; }

    public string? DuplicateOfName { get; set; }

    public int? DuplicateOfRowIndex { get; set; }
}
