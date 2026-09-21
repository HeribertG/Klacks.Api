// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the one advisory sentence that every group-creating skill appends when the run introduces
/// the very first group of the installation: those users keep access to everything until an
/// administrator restricts visibility. Deliberately a single English sentence and no translation key
/// - a skill message is tool output that the model renders in the user's language (the language
/// directive is the first line of the system prompt), so a per-locale catalog entry would never be
/// read. Kept in one place so the preview and the applied message can never say different things.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal static class FirstGroupVisibilityAdvisory
{
    private const string Template =
        "This is the first group of this installation: {0} user(s) keep access to everything until you " +
        "restrict their visibility (user administration, group visibility). Tell the user this.";

    /// <summary>
    /// The advisory sentence, or an empty string when nothing has to be said - either because groups
    /// already existed or because no user is affected.
    /// </summary>
    /// <param name="userCount">Number of users that keep access to everything</param>
    internal static string For(int userCount)
    {
        return userCount > 0 ? string.Format(Template, userCount) : string.Empty;
    }
}
