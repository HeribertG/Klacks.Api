// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Code, detail fields and English text of a container (or container template) that does not hold all of its services
/// including their travel, briefing and debriefing times. The client explains the code in the user's language.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerFitRefusal
{
    public const string Code = "CONTAINER_ITEM_OUTSIDE";

    public const string ContainerField = "container";

    public const string OutsideField = "outside";

    public const string MessageFormat =
        "The container {0} must hold all of its services, absences, travel, briefing and debriefing times; outside: {1}.";
}
