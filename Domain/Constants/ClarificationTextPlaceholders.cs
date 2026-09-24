// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Names of the placeholders the clarification texts carry, written {name} in a template. Named rather
/// than positional so a translator can reorder them for a language whose syntax demands it. The values
/// are ready to insert: times formatted in company time, the original text already shortened.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClarificationTextPlaceholders
{
    public const string Sender = "sender";
    public const string Summary = "summary";
    public const string Question = "question";
    public const string ShiftContext = "shiftContext";
    public const string Deadline = "deadline";
    public const string Asked = "asked";
    public const string OriginalText = "originalText";
    public const string Status = "status";
}
