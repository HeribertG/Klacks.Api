// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpOrderFileRetryResultResource
{
    public Guid Id { get; set; }

    public ErpImportFileStatus Status { get; set; }

    public string? FailureReason { get; set; }
}
