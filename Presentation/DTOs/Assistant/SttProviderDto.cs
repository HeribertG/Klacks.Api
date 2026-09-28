// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Information about an available STT provider.
/// </summary>
/// <param name="ProviderId">Unique provider identifier</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record SttProviderDto(string ProviderId);
