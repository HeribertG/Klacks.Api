// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Machine-readable installation kinds reported with the ERP hand-over advice: native (office PC) or docker.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpHandoverInstallations
{
    public const string Native = "native";
    public const string Docker = "docker";
}
