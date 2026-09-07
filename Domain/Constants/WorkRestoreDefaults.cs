// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Limits for the undo of a Work delete. UndoWindowSeconds bounds how long the deleting user may take
/// the delete back; SiblingDeleteToleranceSeconds is the spread allowed between the delete stamps of a
/// container Work and the rows that were cascaded away in the same save (the context stamps each
/// entry with its own clock read, so the timestamps differ by microseconds, never by seconds).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class WorkRestoreDefaults
{
    public const int UndoWindowSeconds = 60;

    public const int SiblingDeleteToleranceSeconds = 2;
}
