// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.DTOs.Assistant;

public record LocalTtsInstallRequest(bool AcceptLicense, string? LicenseVersion);
