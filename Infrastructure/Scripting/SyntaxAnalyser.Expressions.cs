namespace Klacks.Api.Infrastructure.Scripting
{
    public partial class SyntaxAnalyser
    {
        // OrElseTerm { "OR" OrElseTerm } | OrElseTerm { "ORELSE" OrElseTerm }
        private void Condition()
        {
            OrElseTerm();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokOr, Symbol.Tokens.tokOrElse))
            {
                var isOrElse = sym.Token == Symbol.Tokens.tokOrElse;
                GetNextSymbol();

                if (isOrElse)
                {
                    // OrElse: Short-Circuit - result is True if any operand is truthy
                    var skipPC = code!.Add(Opcodes.JumpTrue);
                    OrElseTerm();
                    var secondTruePC = code.Add(Opcodes.JumpTrue);
                    code.Add(Opcodes.PushValue, false);
                    var endPC = code.Add(Opcodes.Jump);
                    code.FixUp(skipPC - 1, code.EndOfCodePc);
                    code.FixUp(secondTruePC - 1, code.EndOfCodePc);
                    code.Add(Opcodes.PushValue, true);
                    code.FixUp(endPC - 1, code.EndOfCodePc);
                }
                else
                {
                    // Or: Bitwise
                    OrElseTerm();
                    code!.Add(Opcodes.Or);
                }
            }
        }

        // AndAlsoFactor { "AND" AndAlsoFactor } | AndAlsoFactor { "ANDALSO" AndAlsoFactor }
        private void OrElseTerm()
        {
            AndAlsoFactor();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokAnd, Symbol.Tokens.tokAndAlso))
            {
                var isAndAlso = sym.Token == Symbol.Tokens.tokAndAlso;
                GetNextSymbol();

                if (isAndAlso)
                {
                    // AndAlso: Short-Circuit - result is True only if both operands are truthy
                    var skipPC = code!.Add(Opcodes.JumpFalse);
                    AndAlsoFactor();
                    var secondFalsePC = code.Add(Opcodes.JumpFalse);
                    code.Add(Opcodes.PushValue, true);
                    var endPC = code.Add(Opcodes.Jump);
                    code.FixUp(skipPC - 1, code.EndOfCodePc);
                    code.FixUp(secondFalsePC - 1, code.EndOfCodePc);
                    code.Add(Opcodes.PushValue, false);
                    code.FixUp(endPC - 1, code.EndOfCodePc);
                }
                else
                {
                    // And: Bitwise
                    AndAlsoFactor();
                    code!.Add(Opcodes.And);
                }
            }
        }

        // ConditionalFactor (renamed from ConditionalTerm)
        private void AndAlsoFactor()
        {
            ConditionalFactor();
        }

        // Expression { ( "=" | "<>" | "<=" | "<" | ">=" | ">" ) Expression }
        private void ConditionalFactor()
        {
            Symbol.Tokens operator_Renamed;

            Expression();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokEq, Symbol.Tokens.tokNotEq, Symbol.Tokens.tokLEq, Symbol.Tokens.tokLt, Symbol.Tokens.tokGEq, Symbol.Tokens.tokGt))
            {
                operator_Renamed = sym.Token;

                GetNextSymbol();

                Expression();

                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokEq:
                        code!.Add(Opcodes.Eq);
                        break;

                    case Symbol.Tokens.tokNotEq:
                        code!.Add(Opcodes.NotEq);
                        break;

                    case Symbol.Tokens.tokLEq:
                        code!.Add(Opcodes.LEq);
                        break;

                    case Symbol.Tokens.tokLt:
                        code!.Add(Opcodes.Lt);
                        break;

                    case Symbol.Tokens.tokGEq:
                        code!.Add(Opcodes.GEq);
                        break;

                    case Symbol.Tokens.tokGt:
                        code!.Add(Opcodes.Gt);
                        break;
                }
            }
        }

        // Term { ("+" | "-" | "%" | "MOD" | "&") Term }
        private void Expression()
        {
            Symbol.Tokens operator_Renamed;

            Term();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokPlus, Symbol.Tokens.tokMinus, Symbol.Tokens.tokMod, Symbol.Tokens.tokStringConcat))
            {
                operator_Renamed = sym.Token;

                GetNextSymbol();
                Term();

                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokPlus:
                        code!.Add(Opcodes.Add);
                        break;

                    case Symbol.Tokens.tokMinus:
                        code!.Add(Opcodes.Sub);
                        break;

                    case Symbol.Tokens.tokMod:
                        code!.Add(Opcodes.Mod);
                        break;

                    case Symbol.Tokens.tokStringConcat:
                        code!.Add(Opcodes.StringConcat);
                        break;
                }
            }
        }

        // Factor { ("*" | "/" | "\" | "DIV") Factor }
        private void Term()
        {
            Symbol.Tokens operator_Renamed;

            Factor();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokMultiplication, Symbol.Tokens.tokDivision, Symbol.Tokens.tokDiv))
            {
                operator_Renamed = sym.Token;

                GetNextSymbol();
                Factor();

                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokMultiplication:
                        code!.Add(Opcodes.Multiplication);
                        break;

                    case Symbol.Tokens.tokDivision:
                        code!.Add(Opcodes.Division);
                        break;

                    case Symbol.Tokens.tokDiv:
                        code!.Add(Opcodes.Div);
                        break;
                }
            }
        }

        // Factorial [ "^" Factorial ]
        private void Factor()
        {
            Factorial();

            if (sym.Token == Symbol.Tokens.tokPower)
            {
                GetNextSymbol();

                Factorial();

                code!.Add(Opcodes.Power);
            }
        }

        // Terminal [ "!" ]
        private void Factorial()
        {
            Terminal();

            if (sym.Token == Symbol.Tokens.tokFactorial)
            {
                code!.Add(Opcodes.Factorial);

                GetNextSymbol();
            }
        }

        private void Terminal()
        {
            Symbol.Tokens operator_Renamed;
            int thenPC, elsePC;
            string ident;

            switch (sym.Token)
            {
                case Symbol.Tokens.tokMinus:
                    GetNextSymbol();
                    Terminal();
                    code!.Add(Opcodes.Negate);
                    break;

                case Symbol.Tokens.tokNot:
                    GetNextSymbol();
                    Terminal();
                    code!.Add(Opcodes.Not);
                    break;

                case Symbol.Tokens.tokNumber:
                    code!.Add(Opcodes.PushValue, sym.Value!);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokString:
                    code!.Add(Opcodes.PushValue, sym.Value!);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokIdentifier:
                    if (optionExplicit & !symboltable!.Exists(sym.Text))
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.Terminal", "Identifier '" + sym.Text + "' has not be declared", sym.Line, sym.Col, sym.Index, sym.Text);

                    if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.IdFunction))
                    {
                        ident = sym.Text;
                        GetNextSymbol();
                        CallUserdefinedFunction(ident);
                        code!.Add(Opcodes.PushVariable, ident);
                    }
                    else if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.IdSub))
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errCannotCallSubInExpression, "SyntaxAnalyser.Terminal", "Cannot call sub '" + sym.Text + "' in expression", sym.Line, sym.Col, sym.Index);
                    else
                    {
                        code!.Add(Opcodes.PushVariable, sym.Text);
                        GetNextSymbol();
                    }
                    break;

                case Symbol.Tokens.tokTrue:
                    code!.Add(Opcodes.PushValue, true);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokFalse:
                    code!.Add(Opcodes.PushValue, false);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokPi:
                    code!.Add(Opcodes.PushValue, 3.141592654);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokCrlf:
                    code!.Add(Opcodes.PushValue, "\r\n");
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokTab:
                    code!.Add(Opcodes.PushValue, "\t");
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokCr:
                    code!.Add(Opcodes.PushValue, "\r");
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokLf:
                    code!.Add(Opcodes.PushValue, "\n");
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokMsgbox:
                    operator_Renamed = Boxes(sym.Token);
                    break;

                case Symbol.Tokens.tokInputbox:
                    CallInputbox(false);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokMessage:
                    operator_Renamed = Boxes(sym.Token);
                    break;

                case Symbol.Tokens.tokSin:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;

                case Symbol.Tokens.tokCos:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;

                case Symbol.Tokens.tokTan:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;

                case Symbol.Tokens.tokATan:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;

                // String Functions
                case Symbol.Tokens.tokLen:
                    CallUnaryFunction(Opcodes.Len);
                    break;
                case Symbol.Tokens.tokLeft:
                    CallBinaryFunction(Opcodes.Left);
                    break;
                case Symbol.Tokens.tokRight:
                    CallBinaryFunction(Opcodes.Right);
                    break;
                case Symbol.Tokens.tokMid:
                    CallTernaryFunction(Opcodes.Mid);
                    break;
                case Symbol.Tokens.tokInStr:
                    CallBinaryFunction(Opcodes.InStr);
                    break;
                case Symbol.Tokens.tokReplace:
                    CallTernaryFunction(Opcodes.Replace);
                    break;
                case Symbol.Tokens.tokTrim:
                    CallUnaryFunction(Opcodes.Trim);
                    break;
                case Symbol.Tokens.tokUCase:
                    CallUnaryFunction(Opcodes.UCase);
                    break;
                case Symbol.Tokens.tokLCase:
                    CallUnaryFunction(Opcodes.LCase);
                    break;

                // Math Functions
                case Symbol.Tokens.tokAbs:
                    CallUnaryFunction(Opcodes.Abs);
                    break;
                case Symbol.Tokens.tokRound:
                    CallRoundFunction();
                    break;
                case Symbol.Tokens.tokSqr:
                    CallUnaryFunction(Opcodes.Sqr);
                    break;
                case Symbol.Tokens.tokRnd:
                    CallRnd();
                    break;
                case Symbol.Tokens.tokLog:
                    CallUnaryFunction(Opcodes.Log);
                    break;
                case Symbol.Tokens.tokExp:
                    CallUnaryFunction(Opcodes.Exp);
                    break;
                case Symbol.Tokens.tokSgn:
                    CallUnaryFunction(Opcodes.Sgn);
                    break;

                case Symbol.Tokens.tokIif:
                    GetNextSymbol();
                    if (sym.Token == Symbol.Tokens.tokLeftParent)
                    {
                        GetNextSymbol();
                        Condition();
                        thenPC = code!.Add(Opcodes.JumpFalse);

                        if (sym.Token == Symbol.Tokens.tokComma)
                        {
                            GetNextSymbol();
                            Condition();
                            elsePC = code.Add(Opcodes.Jump);
                            code.FixUp(thenPC - 1, code.EndOfCodePc);

                            if (sym.Token == Symbol.Tokens.tokComma)
                            {
                                GetNextSymbol();
                                Condition();
                                code.FixUp(elsePC - 1, code.EndOfCodePc);

                                if (sym.Token == Symbol.Tokens.tokRightParent)
                                    GetNextSymbol();
                                else
                                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after last IIF-parameter", sym.Line, sym.Col, sym.Index, sym.Text);
                            }
                            else
                                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' after true-Value of IIF", sym.Line, sym.Col, sym.Index, sym.Text);
                        }
                        else
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' after IIF-condition", sym.Line, sym.Col, sym.Index, sym.Text);
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after IIF", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokLeftParent:
                    GetNextSymbol();
                    Condition();
                    if (sym.Token == Symbol.Tokens.tokRightParent)
                        GetNextSymbol();
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')'", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokEof:
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Identifier or function or '(' expected but end of source found", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                default:
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Expected: expression; found symbol '" + sym.Text + "'", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;
            }
        }
    }
}
