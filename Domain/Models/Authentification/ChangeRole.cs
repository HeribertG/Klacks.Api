// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Authentification;

public class ChangeRole
{
    public bool IsSelected { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}
