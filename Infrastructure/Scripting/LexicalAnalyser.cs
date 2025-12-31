
using System.Diagnostics;
using static Klacks.Api.Infrastructure.Scripting.InterpreterError;

namespace Klacks.Api.Infrastructure.Scripting
{



    public class LexicalAnalyser
    {
        private const string COMMENT_CHAR = "'";

        private IInputStream? source;
        private readonly Dictionary<string, int> predefinedIdentifiers;
        private InterpreterError? errorObject;




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
                        {
                            nextSymbol.Init(Symbol.Tokens.tokPower, c);
                            break;
                        }

                    case "!":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokFactorial, c);
                            break;
                        }

                    case "~":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokNot, c);
                            break;
                        }

                    case "(":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokLeftParent, c);
                            break;
                        }

                    case ")":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokRightParent, c);
                            break;
                        }

                    case ",":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokComma, c);
                            break;
                        }

                    case "=":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokEq, c);
                            break;
                        }

                    case "<":
                        {
                            c = source.GetNextChar();
                            switch (c)
                            {
                                case ">":
                                    {
                                        nextSymbol.Init(Symbol.Tokens.tokNotEq, "<>");
                                        break;
                                    }

                                case "=":
                                    {
                                        nextSymbol.Init(Symbol.Tokens.tokLEq, "<=");
                                        break;
                                    }

                                default:
                                    {
                                        source.GoBack();

                                        nextSymbol.Init(Symbol.Tokens.tokLt, "<");
                                        break;
                                    }
                            }

                            break;
                        }

                    case ">":
                        {
                            c = source.GetNextChar();
                            switch (c)
                            {
                                case "=":
                                    {
                                        nextSymbol.Init(Symbol.Tokens.tokGEq, ">=");
                                        break;
                                    }
                                default:
                                    {
                                        source.GoBack();

                                        nextSymbol.Init(Symbol.Tokens.tokGt, ">");
                                        break;
                                    }
                            }

                            break;
                        }

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


                        symbolText = "";
                        var endOfEmptyChar = false;
                        do
                        {
                            c = source.GetNextChar();
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
                                        endOfEmptyChar = true;
                                        source.GoBack();
                                        nextSymbol.Init(Symbol.Tokens.tokString, symbolText, symbolText);
                                    }
                                    break;
                                case "\n":
                                    errorObject!.Raise((int)LexErrors.errUnexpectedEOL,
                                      "LexAnalyser.nextSymbol", "String not closed; unexpected end of line encountered",
                                      source.Line, source.Col, source.Index);
                                    endOfEmptyChar = true;
                                    break;


                                case "":
                                    errorObject!.Raise((int)LexErrors.errUnexpectedEOF,
                                      "LexAnalyser.nextSymbol", "String not closed; unexpected end of source",
                                       source.Line, source.Col, source.Index);
                                    endOfEmptyChar = true;
                                    break;
                                default:
                                    symbolText = (symbolText + c);

                                    break;
                            }
                        }
                        while (!endOfEmptyChar);
                        break;

                    case ":":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokStatementDelimiter, c);
                            break;
                        }
                    case "\n":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokStatementDelimiter, c);
                            break;
                        }

                    default:
                        {
                            errorObject!.Raise(Convert.ToInt32(LexErrors.errUnknownSymbol), "LexicalAnalyser.GetNextSymbol", "Unknown symbol starting with character ASCII " + c, nextSymbol.Line, nextSymbol.Col, nextSymbol.Index);
                            break;
                        }
                }
            }
            else
            {
                nextSymbol.Init(Symbol.Tokens.tokEof);
            }

            if (returnNumberSymbol)
            {
                nextSymbol.Init(Symbol.Tokens.tokNumber, symbolText, symbolText);
            }

            return nextSymbol;

        }

        public LexicalAnalyser()
        {
            predefinedIdentifiers = new System.Collections.Generic.Dictionary<string, int>();

            predefinedIdentifiers.Add("DIV", (int)Symbol.Tokens.tokDiv);
            predefinedIdentifiers.Add("MOD", (int)Symbol.Tokens.tokMod);
            predefinedIdentifiers.Add("AND", (int)Symbol.Tokens.tokAnd);
            predefinedIdentifiers.Add("OR", (int)Symbol.Tokens.tokOr);
            predefinedIdentifiers.Add("NOT", (int)Symbol.Tokens.tokNot);
            predefinedIdentifiers.Add("ANDALSO", (int)Symbol.Tokens.tokAndAlso);
            predefinedIdentifiers.Add("ORELSE", (int)Symbol.Tokens.tokOrElse);
            predefinedIdentifiers.Add("SIN", (int)Symbol.Tokens.tokSin);
            predefinedIdentifiers.Add("COS", (int)Symbol.Tokens.tokCos);
            predefinedIdentifiers.Add("TAN", (int)Symbol.Tokens.tokTan);
            predefinedIdentifiers.Add("ATAN", (int)Symbol.Tokens.tokATan);
            predefinedIdentifiers.Add("IIF", (int)Symbol.Tokens.tokIif);
            predefinedIdentifiers.Add("IF", (int)Symbol.Tokens.tokIf);
            predefinedIdentifiers.Add("THEN", (int)Symbol.Tokens.tokThen);
            predefinedIdentifiers.Add("ELSE", (int)Symbol.Tokens.tokElse);
            predefinedIdentifiers.Add("END", (int)Symbol.Tokens.tokEnd);
            predefinedIdentifiers.Add("ENDIF", (int)Symbol.Tokens.tokEndif);
            predefinedIdentifiers.Add("DO", (int)Symbol.Tokens.tokDo);
            predefinedIdentifiers.Add("WHILE", (int)Symbol.Tokens.tokWhile);
            predefinedIdentifiers.Add("LOOP", (int)Symbol.Tokens.tokLoop);
            predefinedIdentifiers.Add("UNTIL", (int)Symbol.Tokens.tokUntil);
            predefinedIdentifiers.Add("FOR", (int)Symbol.Tokens.tokFor);
            predefinedIdentifiers.Add("TO", (int)Symbol.Tokens.tokTo);
            predefinedIdentifiers.Add("STEP", (int)Symbol.Tokens.tokStep);
            predefinedIdentifiers.Add("NEXT", (int)Symbol.Tokens.tokNext);
            predefinedIdentifiers.Add("CONST", (int)Symbol.Tokens.tokConst);
            predefinedIdentifiers.Add("DIM", (int)Symbol.Tokens.tokDim);
            predefinedIdentifiers.Add("FUNCTION", (int)Symbol.Tokens.tokFunction);
            predefinedIdentifiers.Add("ENDFUNCTION", (int)Symbol.Tokens.tokEndfunction);
            predefinedIdentifiers.Add("SUB", (int)Symbol.Tokens.tokSub);
            predefinedIdentifiers.Add("ENDSUB", (int)Symbol.Tokens.tokEndsub);
            predefinedIdentifiers.Add("EXIT", (int)Symbol.Tokens.tokExit);
            predefinedIdentifiers.Add("DEBUGPRINT", (int)Symbol.Tokens.tokDebugPrint);
            predefinedIdentifiers.Add("DEBUGCLEAR", (int)Symbol.Tokens.tokDebugClear);
            predefinedIdentifiers.Add("DEBUGSHOW", (int)Symbol.Tokens.tokDebugShow);
            predefinedIdentifiers.Add("DEBUGHIDE", (int)Symbol.Tokens.tokDebugHide);
            predefinedIdentifiers.Add("MSGBOX", (int)Symbol.Tokens.tokMsgbox);
            predefinedIdentifiers.Add("MESSAGE", (int)Symbol.Tokens.tokMessage);
            predefinedIdentifiers.Add("DOEVENTS", (int)Symbol.Tokens.tokDoEvents);
            predefinedIdentifiers.Add("INPUTBOX", (int)Symbol.Tokens.tokInputbox);
            predefinedIdentifiers.Add("TRUE", (int)Symbol.Tokens.tokTrue);
            predefinedIdentifiers.Add("FALSE", (int)Symbol.Tokens.tokFalse);
            predefinedIdentifiers.Add("PI", (int)Symbol.Tokens.tokPi);
            predefinedIdentifiers.Add("VBCRLF", (int)Symbol.Tokens.tokCrlf);
            predefinedIdentifiers.Add("VBTAB", (int)Symbol.Tokens.tokTab);
            predefinedIdentifiers.Add("VBCR", (int)Symbol.Tokens.tokCr);
            predefinedIdentifiers.Add("VBLF", (int)Symbol.Tokens.tokLf);
            predefinedIdentifiers.Add("IMPORT", (int)Symbol.Tokens.tokExternal);
            // String Functions
            predefinedIdentifiers.Add("LEN", (int)Symbol.Tokens.tokLen);
            predefinedIdentifiers.Add("LEFT", (int)Symbol.Tokens.tokLeft);
            predefinedIdentifiers.Add("RIGHT", (int)Symbol.Tokens.tokRight);
            predefinedIdentifiers.Add("MID", (int)Symbol.Tokens.tokMid);
            predefinedIdentifiers.Add("INSTR", (int)Symbol.Tokens.tokInStr);
            predefinedIdentifiers.Add("REPLACE", (int)Symbol.Tokens.tokReplace);
            predefinedIdentifiers.Add("TRIM", (int)Symbol.Tokens.tokTrim);
            predefinedIdentifiers.Add("UCASE", (int)Symbol.Tokens.tokUCase);
            predefinedIdentifiers.Add("LCASE", (int)Symbol.Tokens.tokLCase);
            // Math Functions
            predefinedIdentifiers.Add("ABS", (int)Symbol.Tokens.tokAbs);
            predefinedIdentifiers.Add("ROUND", (int)Symbol.Tokens.tokRound);
            predefinedIdentifiers.Add("SQR", (int)Symbol.Tokens.tokSqr);
            predefinedIdentifiers.Add("RND", (int)Symbol.Tokens.tokRnd);
            predefinedIdentifiers.Add("LOG", (int)Symbol.Tokens.tokLog);
            predefinedIdentifiers.Add("EXP", (int)Symbol.Tokens.tokExp);
            predefinedIdentifiers.Add("SGN", (int)Symbol.Tokens.tokSgn);
            predefinedIdentifiers.Add("TIMETOHOURS", (int)Symbol.Tokens.tokTimeToHours);
            predefinedIdentifiers.Add("TIMEOVERLAP", (int)Symbol.Tokens.tokTimeOverlap);
            // Control Structures
            predefinedIdentifiers.Add("SELECT", (int)Symbol.Tokens.tokSelect);
            predefinedIdentifiers.Add("CASE", (int)Symbol.Tokens.tokCase);
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
