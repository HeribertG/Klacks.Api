// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityBulkRequest
{
    public List<ClientAvailabilityResource> Items { get; set; } = [];
}
