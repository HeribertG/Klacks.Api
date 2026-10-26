// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Text the model receives as the result of a UiAction skill. The server only hands the steps to the browser, so the
/// text must not let the model describe what the page shows afterwards.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class UiActionResultMessages
{
    public const string DispatchedFormat =
        "Function '{0}' will be executed as UI action in the user's browser. " +
        "The browser's result is not part of this reply: name only what was set (the parameters you passed) and " +
        "never state what the page or list now shows, or that it is limited to a group, location or other subset.";
}
