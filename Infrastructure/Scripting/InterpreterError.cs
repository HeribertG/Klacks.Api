// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fehler-Objekt fuer alle Klassen der myScript-Engine.
/// Ueber Raise() werden die Fehlerparameter gesetzt und ein VB-Fehler
/// ausgeloest. Alle Klassen der Engine sind so ausgerichtet, dass sie
/// bei einem VB-Fehler komplett zurueckfallen zur Aufrufstelle des
/// Parsers bzw. Interpreters, d.h. jeder Syntaxfehler usw. ist fuer
/// den Script-Host ein trappable error.
/// </summary>
/// <param name="info">Enthaelt Number, Source, Description, Line, Col, Index und optionalen ErrSource</param>

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
