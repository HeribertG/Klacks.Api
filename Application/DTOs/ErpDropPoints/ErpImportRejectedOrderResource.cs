// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpImportRejectedOrderResource
{
    public string ExternalOrderReference { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
