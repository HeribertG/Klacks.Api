// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Status of the local voice for the speech settings card: whether the updater registered the Klacks.Stt address,
/// whether an installation is queued or running, the message of the last failed installation, whether the current
/// licence version was accepted and when, and the download size of the model.
/// </summary>
namespace Klacks.Api.Application.DTOs.Assistant;

public record LocalTtsStatus(
    bool Installed,
    bool Pending,
    string? LastError,
    bool LicenseAccepted,
    string? LicenseAcceptedAt,
    long SizeBytes);
