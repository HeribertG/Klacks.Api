// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Seed;

public class DemoOrderCustomer
{
    public int IdNumber { get; init; }

    public string Company { get; init; } = string.Empty;

    public string Street { get; init; } = string.Empty;

    public string Zip { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}
