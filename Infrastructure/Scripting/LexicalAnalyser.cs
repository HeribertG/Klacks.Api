// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lexikalische Analyse: Zerlegt den Quelltext-Stream in Symbole (Tokens).
/// <param name="source">Der Eingabe-Stream, aus dem Zeichen gelesen werden</param>
/// <param name="predefinedIdentifiers">Statisches Dictionary mit Keywords und deren Token-Zuordnung</param>
/// </summary>

using System.Diagnostics;
using static Klacks.Api.Infrastructure.Scripting.InterpreterError;

namespace Klacks.Api.Infrastructure.Scripting
{
    public class LexicalAnalyser
    {
        private const string COMMENT_CHAR = "'";

        private IInputStream? source;
        private static readonly Dictionary<string, int> predefinedIdentifiers = InitializeKeywords();
        private InterpreterError? errorObject;

        public LexicalAnalyser()
        {
        }

        public LexicalAnalyser Connect(IInputStream source, InterpreterError errorObject)
        {
            this.source = source;
            this.errorObject = errorObject;

            return this;
        }

        public InterpreterError ErrorObject
        {
            set
            {
                try
                {
                    if (source != null)
                    {
                        errorObject = value;
                        source.ErrorObject = errorObject;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("LexicalAnalyser.ErrorObject " + ex.Message);
                }
            }
        }

        public Symbol GetNextSymbol()
        {
            var nextSymbol = new Symbol();

            string c = string.Empty;
            string symbolText = string.Empty;
            bool returnNumberSymbol = false;

            if (!source!.Eof)
            {
                do
                {
                    c = source.GetNextChar();
                    if (c == COMMENT_CHAR)
                    {
                        source.SkipComment();
                        c = " ";
                    }
                }
                while (c == " " & !source.Eof);
            }

            nextSymbol.Position(source.Line, source.Col, source.Index);

            if (!source.Eof)
            {
                switch (c.ToUpper())
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "&":
                    case "\\":
                    case "%":
                        MathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "^":
                        nextSymbol.Init(Symbol.Tokens.tokPower, c);
                        break;
                    case "!":
                        nextSymbol.Init(Symbol.Tokens.tokFactorial, c);
                        break;
                    case "~":
                        nextSymbol.Init(Symbol.Tokens.tokNot, c);
                        break;
                    case "(":
                        nextSymbol.Init(Symbol.Tokens.tokLeftParent, c);
                        break;
                    case ")":
                        nextSymbol.Init(Symbol.Tokens.tokRightParent, c);
                        break;
                    case ",":
                        nextSymbol.Init(Symbol.Tokens.tokComma, c);
                        break;
                    case "=":
                        nextSymbol.Init(Symbol.Tokens.tokEq, c);
                        break;
                    case "<":
                        LexLessThan(nextSymbol);
                        break;
                    case ">":
                        LexGreaterThan(nextSymbol);
                        break;
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                        var res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "@":
                    case "A":
                    case "B":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "H":
                    case "I":
                    case "J":
                    case "K":
                    case "L":
                    case "M":
                    case "N":
                    case "O":
                    case "P":
                    case "Q":
                    case "R":
                    case "S":
                    case "T":
                    case "U":
                    case "V":
                    case "W":
                    case "X":
                    case "Y":
                    case "Z":
                    case "Ä":
                    case "Ö":
                    case "Ü":
                    case "ß":
                    case "_":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "\"":
                        LexString(nextSymbol);
                        break;
                    case ":":
                    case "\n":
                        nextSymbol.Init(Symbol.Tokens.tokStatementDelimiter, c);
                        break;
                    default:
                        errorObject!.Raise(new InterpreterErrorInfo(Convert.ToInt32(LexErrors.errUnknownSymbol), "LexicalAnalyser.GetNextSymbol", "Unknown symbol starting with character ASCII " + c, nextSymbol.Line, nextSymbol.Col, nextSymbol.Index));
                        break;
                }
            }
            else
            {
                nextSymbol.Init(Symbol.Tokens.tokEof);
            }

            if (returnNumberSymbol)
            {
                nextSymbol.Init(Symbol.Tokens.tokNumber, symbolText, double.Parse(symbolText, System.Globalization.CultureInfo.InvariantCulture));
            }

            return nextSymbol;
        }

        private void LexLessThan(Symbol nextSymbol)
        {
            var c = source!.GetNextChar();
            switch (c)
            {
                case ">":
                    nextSymbol.Init(Symbol.Tokens.tokNotEq, "<>");
                    break;
                case "=":
                    nextSymbol.Init(Symbol.Tokens.tokLEq, "<=");
                    break;
                default:
                    source.GoBack();
                    nextSymbol.Init(Symbol.Tokens.tokLt, "<");
                    break;
            }
        }

        private void LexGreaterThan(Symbol nextSymbol)
        {
            var c = source!.GetNextChar();
            switch (c)
            {
                case "=":
                    nextSymbol.Init(Symbol.Tokens.tokGEq, ">=");
                    break;
                default:
                    source.GoBack();
                    nextSymbol.Init(Symbol.Tokens.tokGt, ">");
                    break;
            }
        }

        private void LexString(Symbol nextSymbol)
        {
            var symbolText = "";
            var endOfString = false;
            do
            {
                var c = source!.GetNextChar();
                switch (c)
                {
                    case "\"":
                        c = source.GetNextChar();
                        if (c == "\"")
                        {
                            symbolText += "\"";
                        }
                        else
                        {
                            endOfString = true;
                            source.GoBack();
                            nextSymbol.Init(Symbol.Tokens.tokString, symbolText, symbolText);
                        }
                        break;
                    case "\n":
                        errorObject!.Raise(new InterpreterErrorInfo(
                          (int)LexErrors.errUnexpectedEOL,
                          "LexAnalyser.nextSymbol", "String not closed; unexpected end of line encountered",
                          source.Line, source.Col, source.Index));
                        endOfString = true;
                        break;
                    case "":
                        errorObject!.Raise(new InterpreterErrorInfo(
                          (int)LexErrors.errUnexpectedEOF,
                          "LexAnalyser.nextSymbol", "String not closed; unexpected end of source",
                           source.Line, source.Col, source.Index));
                        endOfString = true;
                        break;
                    default:
                        symbolText = (symbolText + c);
                        break;
                }
            }
            while (!endOfString);
        }

        private static Dictionary<string, int> InitializeKeywords()
        {
            return new Dictionary<string, int>
            {
                { "DIV", (int)Symbol.Tokens.tokDiv },
                { "MOD", (int)Symbol.Tokens.tokMod },
                { "AND", (int)Symbol.Tokens.tokAnd },
                { "OR", (int)Symbol.Tokens.tokOr },
                { "NOT", (int)Symbol.Tokens.tokNot },
                { "ANDALSO", (int)Symbol.Tokens.tokAndAlso },
                { "ORELSE", (int)Symbol.Tokens.tokOrElse },
                { "SIN", (int)Symbol.Tokens.tokSin },
                { "COS", (int)Symbol.Tokens.tokCos },
                { "TAN", (int)Symbol.Tokens.tokTan },
                { "ATAN", (int)Symbol.Tokens.tokATan },
                { "IIF", (int)Symbol.Tokens.tokIif },
                { "IF", (int)Symbol.Tokens.tokIf },
                { "THEN", (int)Symbol.Tokens.tokThen },
                { "ELSE", (int)Symbol.Tokens.tokElse },
                { "END", (int)Symbol.Tokens.tokEnd },
                { "ENDIF", (int)Symbol.Tokens.tokEndif },
                { "DO", (int)Symbol.Tokens.tokDo },
                { "WHILE", (int)Symbol.Tokens.tokWhile },
                { "LOOP", (int)Symbol.Tokens.tokLoop },
                { "UNTIL", (int)Symbol.Tokens.tokUntil },
                { "FOR", (int)Symbol.Tokens.tokFor },
                { "TO", (int)Symbol.Tokens.tokTo },
                { "STEP", (int)Symbol.Tokens.tokStep },
                { "NEXT", (int)Symbol.Tokens.tokNext },
                { "CONST", (int)Symbol.Tokens.tokConst },
                { "DIM", (int)Symbol.Tokens.tokDim },
                { "FUNCTION", (int)Symbol.Tokens.tokFunction },
                { "ENDFUNCTION", (int)Symbol.Tokens.tokEndfunction },
                { "SUB", (int)Symbol.Tokens.tokSub },
                { "ENDSUB", (int)Symbol.Tokens.tokEndsub },
                { "EXIT", (int)Symbol.Tokens.tokExit },
                { "DEBUGPRINT", (int)Symbol.Tokens.tokDebugPrint },
                { "DEBUGCLEAR", (int)Symbol.Tokens.tokDebugClear },
                { "DEBUGSHOW", (int)Symbol.Tokens.tokDebugShow },
                { "DEBUGHIDE", (int)Symbol.Tokens.tokDebugHide },
                { "MSGBOX", (int)Symbol.Tokens.tokMsgbox },
                { "OUTPUT", (int)Symbol.Tokens.tokOutput },
                { "DOEVENTS", (int)Symbol.Tokens.tokDoEvents },
                { "INPUTBOX", (int)Symbol.Tokens.tokInputbox },
                { "TRUE", (int)Symbol.Tokens.tokTrue },
                { "FALSE", (int)Symbol.Tokens.tokFalse },
                { "PI", (int)Symbol.Tokens.tokPi },
                { "VBCRLF", (int)Symbol.Tokens.tokCrlf },
                { "VBTAB", (int)Symbol.Tokens.tokTab },
                { "VBCR", (int)Symbol.Tokens.tokCr },
                { "VBLF", (int)Symbol.Tokens.tokLf },
                { "IMPORT", (int)Symbol.Tokens.tokExternal },
                { "LEN", (int)Symbol.Tokens.tokLen },
                { "LEFT", (int)Symbol.Tokens.tokLeft },
                { "RIGHT", (int)Symbol.Tokens.tokRight },
                { "MID", (int)Symbol.Tokens.tokMid },
                { "INSTR", (int)Symbol.Tokens.tokInStr },
                { "REPLACE", (int)Symbol.Tokens.tokReplace },
                { "TRIM", (int)Symbol.Tokens.tokTrim },
                { "UCASE", (int)Symbol.Tokens.tokUCase },
                { "LCASE", (int)Symbol.Tokens.tokLCase },
                { "ABS", (int)Symbol.Tokens.tokAbs },
                { "ROUND", (int)Symbol.Tokens.tokRound },
                { "SQR", (int)Symbol.Tokens.tokSqr },
                { "RND", (int)Symbol.Tokens.tokRnd },
                { "LOG", (int)Symbol.Tokens.tokLog },
                { "EXP", (int)Symbol.Tokens.tokExp },
                { "SGN", (int)Symbol.Tokens.tokSgn },
                { "TIMETOHOURS", (int)Symbol.Tokens.tokTimeToHours },
                { "TIMEOVERLAP", (int)Symbol.Tokens.tokTimeOverlap },
                { "SELECT", (int)Symbol.Tokens.tokSelect },
                { "CASE", (int)Symbol.Tokens.tokCase },
            };
        }

        private void MathOperatorOrAssignments(Symbol nextSymbol, string c)
        {
            var symbolText = c;
            c = source!.GetNextChar();

            if ((c == "="))
            {
                symbolText = (symbolText + c);
            }

            switch (symbolText.Substring(0, 1))
            {
                case "+":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokPlusEq : Symbol.Tokens.tokPlus, symbolText);
                    break;
                case "-":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokMinusEq : Symbol.Tokens.tokMinus, symbolText);
                    break;
                case "*":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokMultiplicationEq : Symbol.Tokens.tokMultiplication, symbolText);
                    break;
                case "/":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokDivisionEq : Symbol.Tokens.tokDivision, symbolText);
                    break;
                case "&":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokStringConcatEq : Symbol.Tokens.tokStringConcat, symbolText);
                    break;
                case "\\":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokDivEq : Symbol.Tokens.tokDiv, symbolText);
                    break;
                case "%":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokModEq : Symbol.Tokens.tokMod, symbolText);
                    break;
            }

            if (c != "=") { source.GoBack(); }
        }

        private Tuple<bool, string> Numbers(Symbol nextSymbol, string c)
        {
            var symbolText = c;
            var returnNumberSymbol = false;

            do
            {
                c = source!.GetNextChar();
                if (Helper.IsNumericInt(c) && ((int.Parse(c) >= 0) && (int.Parse(c) <= 9)))
                {
                    symbolText = (symbolText + c);
                }
                else if ((c == "."))
                {
                    symbolText = (symbolText + ".");
                    for (
                      ; true;
                    )
                    {
                        c = source.GetNextChar();
                        if (Helper.IsNumericInt(c) && ((int.Parse(c) >= 0) && (int.Parse(c) <= 9)))
                        {
                            symbolText = (symbolText + c);
                        }
                        else
                        {
                            source.GoBack();
                            returnNumberSymbol = true;
                            break;
                        }

                    }

                    break;
                }
                else
                {
                    source.GoBack();
                    returnNumberSymbol = true;
                    break;
                }
            } while (source.Eof == false);

            return new Tuple<bool, string>(returnNumberSymbol, symbolText);
        }

        private string Identifier(Symbol nextSymbol, string c)
        {

            string symbolText = c;
            bool breakLoop = false;

            c = source!.GetNextChar();
            do
            {
                switch (c.ToUpper())
                {
                    case "@":
                    case "A":
                    case "B":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "G":
                    case "H":
                    case "I":
                    case "J":
                    case "K":
                    case "L":
                    case "M":
                    case "N":
                    case "O":
                    case "P":
                    case "Q":
                    case "R":
                    case "S":
                    case "T":
                    case "U":
                    case "V":
                    case "W":
                    case "X":
                    case "Y":
                    case "Z":
                    case "Ä":
                    case "Ö":
                    case "Ü":
                    case "ß":
                    case "_":
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                        symbolText += c;
                        break;
                    default:
                        source.GoBack();
                        breakLoop = true;
                        if (predefinedIdentifiers.ContainsKey(symbolText.ToUpper()))
                        {
                            nextSymbol.Init((Symbol.Tokens)predefinedIdentifiers[symbolText.ToUpper()], symbolText);
                        }
                        else
                        {
                            nextSymbol.Init(Symbol.Tokens.tokIdentifier, symbolText);
                        }
                        break;
                }

                if (source.Eof)
                {
                    breakLoop = true;
                }

                if (!breakLoop)
                {
                    c = source.GetNextChar();
                }
            }
            while (!breakLoop);

            return symbolText;
        }

        public void Dispose()
        {
            errorObject = null;
            source = null;

        }

        ~LexicalAnalyser()
        {
            Dispose();
        }
    }
}
