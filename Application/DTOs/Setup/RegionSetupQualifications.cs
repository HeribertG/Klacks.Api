// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupQualifications
{
    public bool? ExpiredMandatoryBlocks { get; set; }

    public int? ExpiryWarningDays { get; set; }
}
