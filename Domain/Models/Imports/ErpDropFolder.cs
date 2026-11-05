// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Where the default ERP drop point folder lies and how an ERP gets files into it.
/// </summary>
/// <param name="FolderPath">Absolute folder as the Klacks process sees it (inside the container on Docker)</param>
/// <param name="HostFolderPath">Docker only: the same folder on the host, null when unknown or on a native installation</param>
/// <param name="Guidance">Install-dependent hand-over advice from ErpHandoverAdvisor</param>
namespace Klacks.Api.Domain.Models.Imports;

public sealed record ErpDropFolder(string FolderPath, string? HostFolderPath, ErpHandoverGuidance Guidance);
