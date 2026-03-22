// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Error object for all classes of the myScript engine.
/// Via Raise() the error parameters are set and a VB error
/// is raised. All engine classes are designed to
/// fall back completely to the call site of the
/// parser or interpreter, i.e. every syntax error etc. is a
/// trappable error for the script host.
/// </summary>
/// <param name="info">Contains Number, Source, Description, Line, Col, Index and optional ErrSource</param>

namespace Klacks.Api.Infrastructure.Scripting;

public record InterpreterErrorInfo(
    int Number,
    string Source,
    string Description,
    int Line,
    int Col,
    int Index,
    string ErrSource = "");

public class InterpreterError
{
    const int OBJECTERROR = -2147221504;
    public enum InputStreamErrors
    {
        errGoBackPastStartOfSource = OBJECTERROR + 1,
        errInvalidChar = OBJECTERROR + 2,
        errGoBackNotImplemented = OBJECTERROR + 3
    }

    public enum LexErrors
    {
        errUnknownSymbol = OBJECTERROR + 21,
        errUnexpectedEOF = OBJECTERROR + 22,
        errUnexpectedEOL = OBJECTERROR + 23
    }

    public enum ParsErrors
    {
        errMissingClosingParent = OBJECTERROR + 31,
        errUnexpectedSymbol = OBJECTERROR + 32,
        errMissingLeftParent = OBJECTERROR + 33,
        errMissingComma = OBJECTERROR + 34,
        errNoYetImplemented = OBJECTERROR + 35,
        errSyntaxViolation = OBJECTERROR + 36,
        errIdentifierAlreadyExists = OBJECTERROR + 37,
        errWrongNumberOfParams = OBJECTERROR + 38,
        errCannotCallSubInExpression = OBJECTERROR + 39
    }

    public enum RunErrors
    {
        errMath = OBJECTERROR + 61,
        errTimedOut = OBJECTERROR + 62,
        errCancelled = OBJECTERROR + 63,
        errNoUIallowed = OBJECTERROR + 64,
        errUninitializedVar = OBJECTERROR + 65,
        errUnknownVar = OBJECTERROR + 66
    }

    public void Raise(InterpreterErrorInfo info)
    {
        Number = info.Number;
        Source = info.Source;
        Description = info.Description;
        Line = info.Line;
        Col = info.Col;
        Index = info.Index;
        ErrSource = info.ErrSource;
    }

    public void Clear()
    {
        Number = 0;
        Source = string.Empty;
        Description = string.Empty;
        Line = 0;
        Col = 0;
        Index = 0;
        ErrSource = string.Empty;
    }

    public int Index { get; private set; }
    public int Number { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Line { get; private set; }
    public int Col { get; private set; }
    public string ErrSource { get; private set; } = string.Empty;
}
