// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Step vocabulary of a UiAction handler config that the server has to recognise: the property that names a step's
/// action and the apiCall action, which makes the browser post an arbitrary same-origin request with the user's own
/// session. No seed and no plugin ships such a step, so neither a model-created skill nor a stored config may carry one.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class UiActionStepActions
{
    public const string ActionProperty = "action";

    public const string ApiCall = "apiCall";

    public const string ApiCallRejectedMessage =
        "UiAction steps of type 'apiCall' are not allowed: a UiAction skill may only drive the user interface " +
        "(navigate, click, fill fields), never post requests with the user's session.";
}
