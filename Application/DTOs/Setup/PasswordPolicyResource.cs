// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Setup;

public class PasswordPolicyResource
{
    public int RequiredLength { get; set; }

    public bool RequireDigit { get; set; }

    public bool RequireNonAlphanumeric { get; set; }

    public bool RequireUppercase { get; set; }

    public bool RequireLowercase { get; set; }
}
