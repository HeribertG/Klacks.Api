// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

public static class LocalTtsReasons
{
    public const string InstallEnqueued = "Local voice installation enqueued.";
    public const string LicenseNotAccepted = "The licence of the local voice model must be accepted before the installation.";
    public const string LicenseVersionMismatch = "The accepted licence version does not match the current licence of the local voice model.";
    public const string InstallAlreadyPending = "An installation of the local voice is already pending.";
}
