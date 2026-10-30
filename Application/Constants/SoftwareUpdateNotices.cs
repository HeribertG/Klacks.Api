// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// The sentences the update preview, the update skill and the update advice share, so all three say the same thing
/// about what a running update does to the people using Klacks.
/// </summary>
public static class SoftwareUpdateNotices
{
    public const string Restart =
        "Klacks restarts during the update: this chat is interrupted and everyone signed in is disconnected, without any warning to them.";

    public const string AutomaticRestore =
        "If the new version does not come up healthy, the previous version is restored automatically.";

    public const string ResultOnCard =
        "The outcome appears in the record on the Software Updates card in the settings.";
}
