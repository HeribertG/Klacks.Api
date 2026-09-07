// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The route the setup consultation resolved for one installation and one pair of answers, plus what
/// still has to exist before that route can be walked. Purely descriptive: the resolver names the
/// route, the skill turns it into navigation or a handoff, and the conversation puts it into words.
/// </summary>
/// <param name="Kind">Which of the routes applies.</param>
/// <param name="ShowTarget">Navigation target id for "just show me where", from navigation-targets.json.</param>
/// <param name="MissingPrerequisites">Prerequisite names still absent, empty when the route is walkable.</param>
/// <param name="HandoffPhrase">Exact trigger phrase of the follow-up recipe, null when none exists.</param>
/// <param name="RequiresAdmin">True when only an administrator can take this route.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SetupRouteFacts(
    SetupRouteKind Kind,
    string ShowTarget,
    IReadOnlyList<string> MissingPrerequisites,
    string? HandoffPhrase,
    bool RequiresAdmin);
