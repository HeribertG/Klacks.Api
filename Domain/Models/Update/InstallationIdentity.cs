// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How this installation is deployed; decides which manifest artifact an update may use.
/// </summary>
/// <param name="Kind">Docker stack or native installation (Windows service, launchd)</param>
/// <param name="RuntimeIdentifier">.NET runtime identifier of the running process, e.g. win-x64 or osx-arm64</param>

namespace Klacks.Api.Domain.Models.Update;

public sealed record InstallationIdentity(InstallationKind Kind, string RuntimeIdentifier);
