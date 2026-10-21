// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Dashboard;

public class ClientLocationResource
{
    public string Id { get; set; } = string.Empty;
    public int Type { get; set; }
    public AddressInfo? CurrentAddress { get; set; }
}
