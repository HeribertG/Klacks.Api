// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Keys and fixed values of the arguments an import issue carries for its translated message.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClientImportIssueArgs
{
    public const string Value = "value";
    public const string Reason = "reason";
    public const string Name = "name";
    public const string Date = "date";
    public const string Row = "row";
    public const string MaxLength = "maxLength";
    public const string ReasonExitBeforeEntry = "exit-before-entry";
    public const string ReasonFuture = "future";
    public const string ReasonAmbiguous = "ambiguous";
    public const string ReasonNotSplit = "not-split";
}
