// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
            switch (sym.Token)
            {
                case Symbol.Tokens.tokMinus:
                case Symbol.Tokens.tokNot:
                    ParseUnaryOperator();
                    break;

                case Symbol.Tokens.tokNumber:
                case Symbol.Tokens.tokString:
                    ParseLiteral();
                    break;

                case Symbol.Tokens.tokIdentifier:
                    ParseIdentifierOrFunctionCall();
                    break;

                case Symbol.Tokens.tokTrue:
                case Symbol.Tokens.tokFalse:
                    ParseBooleanLiteral();
                    break;

                case Symbol.Tokens.tokPi:
                case Symbol.Tokens.tokCrlf:
                case Symbol.Tokens.tokTab:
                case Symbol.Tokens.tokCr:
                case Symbol.Tokens.tokLf:
                    ParseConstantLiteral();
                    break;

                case Symbol.Tokens.tokMsgbox:
                case Symbol.Tokens.tokOutput:
                    Boxes(sym.Token);
                    break;

                case Symbol.Tokens.tokInputbox:
                    CallInputbox(false);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokSin:
                case Symbol.Tokens.tokCos:
                case Symbol.Tokens.tokTan:
                case Symbol.Tokens.tokATan:
                    ComplexGeometry(sym.Token);
                    break;

                case Symbol.Tokens.tokLen:
                case Symbol.Tokens.tokLeft:
                case Symbol.Tokens.tokRight:
                case Symbol.Tokens.tokMid:
                case Symbol.Tokens.tokInStr:
                case Symbol.Tokens.tokReplace:
                case Symbol.Tokens.tokTrim:
                case Symbol.Tokens.tokUCase:
                case Symbol.Tokens.tokLCase:
                    ParseStringFunction();
                    break;

                case Symbol.Tokens.tokAbs:
                case Symbol.Tokens.tokRound:
                case Symbol.Tokens.tokSqr:
                case Symbol.Tokens.tokRnd:
                case Symbol.Tokens.tokLog:
                case Symbol.Tokens.tokExp:
                case Symbol.Tokens.tokSgn:
                case Symbol.Tokens.tokTimeToHours:
                case Symbol.Tokens.tokTimeOverlap:
                    ParseMathFunction();
                    break;

                case Symbol.Tokens.tokIif:
                    ParseIIfExpression();
                    break;

                case Symbol.Tokens.tokLeftParent:
                    ParseParenthesizedExpression();
                    break;

                case Symbol.Tokens.tokEof:
                    errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Identifier or function or '(' expected but end of source found", sym.Line, sym.Col, sym.Index, sym.Text));
                    break;

                default:
                    errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Expected: expression; found symbol '" + sym.Text + "'", sym.Line, sym.Col, sym.Index, sym.Text));
                    break;
            }
        }

        private void ParseUnaryOperator()
        {
            var token = sym.Token;
            GetNextSymbol();
            Terminal();
            code!.Add(token == Symbol.Tokens.tokMinus ? Opcodes.Negate : Opcodes.Not);
        }

        private void ParseLiteral()
        {
            code!.Add(Opcodes.PushValue, sym.Value!);
            GetNextSymbol();
        }

        private void ParseIdentifierOrFunctionCall()
        {
            if (optionExplicit & !symboltable!.Exists(sym.Text))
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.Terminal", "Identifier '" + sym.Text + "' has not be declared", sym.Line, sym.Col, sym.Index, sym.Text));

            bool isCurrentFunction = currentFunctionName != null &&
                string.Equals(sym.Text, currentFunctionName, StringComparison.OrdinalIgnoreCase);

            if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.IdFunction))
            {
                var ident = sym.Text;
                GetNextSymbol();

                if (isCurrentFunction && sym.Token != Symbol.Tokens.tokLeftParent)
                {
                    code!.Add(Opcodes.PushVariable, ident);
                }
                else
                {
                    CallUserdefinedFunction(ident);
                    code!.Add(Opcodes.PushVariable, ident);
                }
            }
            else if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.IdSub))
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errCannotCallSubInExpression, "SyntaxAnalyser.Terminal", "Cannot call sub '" + sym.Text + "' in expression", sym.Line, sym.Col, sym.Index));
            else
            {
                code!.Add(Opcodes.PushVariable, sym.Text);
                GetNextSymbol();
            }
        }

        private void ParseBooleanLiteral()
        {
            code!.Add(Opcodes.PushValue, sym.Token == Symbol.Tokens.tokTrue);
            GetNextSymbol();
        }

        private void ParseConstantLiteral()
        {
            object value = sym.Token switch
            {
                Symbol.Tokens.tokPi => 3.141592654,
                Symbol.Tokens.tokCrlf => "\r\n",
                Symbol.Tokens.tokTab => "\t",
                Symbol.Tokens.tokCr => "\r",
                Symbol.Tokens.tokLf => "\n",
                _ => throw new InvalidOperationException("Unexpected constant token: " + sym.Token)
            };

            code!.Add(Opcodes.PushValue, value);
            GetNextSymbol();
        }

        private void ParseStringFunction()
        {
            switch (sym.Token)
            {
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
            }
        }

        private void ParseMathFunction()
        {
            switch (sym.Token)
            {
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
                case Symbol.Tokens.tokTimeToHours:
                    CallUnaryFunction(Opcodes.TimeToHours);
                    break;
                case Symbol.Tokens.tokTimeOverlap:
                    CallQuaternaryFunction(Opcodes.TimeOverlap);
                    break;
            }
        }

        private void ParseIIfExpression()
        {
            GetNextSymbol();
            if (sym.Token != Symbol.Tokens.tokLeftParent)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after IIF", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();
            Condition();
            var thenPC = code!.Add(Opcodes.JumpFalse);

            if (sym.Token != Symbol.Tokens.tokComma)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' after IIF-condition", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();
            Condition();
            var elsePC = code.Add(Opcodes.Jump);
            code.FixUp(thenPC - 1, code.EndOfCodePc);

            if (sym.Token != Symbol.Tokens.tokComma)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' after true-Value of IIF", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();
            Condition();
            code.FixUp(elsePC - 1, code.EndOfCodePc);

            if (sym.Token == Symbol.Tokens.tokRightParent)
                GetNextSymbol();
            else
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after last IIF-parameter", sym.Line, sym.Col, sym.Index, sym.Text));
        }

        private void ParseParenthesizedExpression()
        {
            GetNextSymbol();
            Condition();
            if (sym.Token == Symbol.Tokens.tokRightParent)
                GetNextSymbol();
            else
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')'", sym.Line, sym.Col, sym.Index, sym.Text));
        }
    }
}
