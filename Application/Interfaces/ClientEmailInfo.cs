// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public class ClientEmailInfo
{
    public Guid ClientId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string ClientDisplayName { get; set; } = string.Empty;
    public List<Guid> GroupIds { get; set; } = [];
}
