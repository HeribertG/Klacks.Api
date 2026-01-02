namespace Klacks.Api.Infrastructure.Scripting
{
    public partial class SyntaxAnalyser
    {
        private Symbol.Tokens Boxes(Symbol.Tokens operator_Renamed)
        {
            GetNextSymbol();

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();

                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokMsgbox:
                        CallMsgBox(false);
                        break;

                    case Symbol.Tokens.tokInputbox:
                        CallInputbox(false);
                        break;

                    case Symbol.Tokens.tokOutput:
                        CallMsg(false);
                        break;
                }

                if (sym.Token == Symbol.Tokens.tokRightParent)
                {
                    GetNextSymbol();
                }
                else
                {
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                }
            }
            else
            {
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' in function call", sym.Line, sym.Col, sym.Index, sym.Text);
            }

            return operator_Renamed;
        }

        private void CallMsgBox(bool dropReturnValue)
        {
            ActualOptionalParameter(string.Empty);
            ActualOptionalParameter(0);
            ActualOptionalParameter(string.Empty);

            code!.Add(Opcodes.Msgbox);

            if (dropReturnValue)
            {
                code.Add(Opcodes.Pop);
            }
        }

        private void CallInputbox(bool dropReturnValue)
        {
            ActualOptionalParameter(string.Empty);
            ActualOptionalParameter(string.Empty);
            ActualOptionalParameter(string.Empty);

            code!.Add(Opcodes.Inputbox);

            if (dropReturnValue)
            {
                code.Add(Opcodes.Pop);
            }
        }

        private void CallMsg(bool dropReturnValue)
        {
            ActualOptionalParameter(0);
            ActualOptionalParameter(string.Empty);

            code!.Add(Opcodes.Message);

            if (dropReturnValue)
            {
                code.Add(Opcodes.Pop);
            }
        }

        // ( "SIN" | "COS" | "TAN" | "ATAN" ) "(" Condition ")"
        private Symbol.Tokens ComplexGeometry(Symbol.Tokens operator_Renamed)
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();

                Condition();

                if (sym.Token == Symbol.Tokens.tokRightParent)
                {
                    GetNextSymbol();

                    switch (operator_Renamed)
                    {
                        case Symbol.Tokens.tokSin:
                            code!.Add(Opcodes.Sin);
                            break;

                        case Symbol.Tokens.tokCos:
                            code!.Add(Opcodes.Cos);
                            break;

                        case Symbol.Tokens.tokTan:
                            code!.Add(Opcodes.Tan);
                            break;

                        case Symbol.Tokens.tokATan:
                            code!.Add(Opcodes.ATan);
                            break;
                    }
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameter", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function Name", sym.Line, sym.Col, sym.Index, sym.Text);

            return operator_Renamed;
        }

        private void CallUnaryFunction(Opcodes opcode)
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();
                Condition();
                if (sym.Token == Symbol.Tokens.tokRightParent)
                {
                    GetNextSymbol();
                    code!.Add(opcode);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameter", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function name", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void CallBinaryFunction(Opcodes opcode)
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();
                Condition();
                if (sym.Token == Symbol.Tokens.tokComma)
                {
                    GetNextSymbol();
                    Condition();
                    if (sym.Token == Symbol.Tokens.tokRightParent)
                    {
                        GetNextSymbol();
                        code!.Add(opcode);
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' between function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function name", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void CallTernaryFunction(Opcodes opcode)
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();
                Condition();
                if (sym.Token == Symbol.Tokens.tokComma)
                {
                    GetNextSymbol();
                    Condition();
                    if (sym.Token == Symbol.Tokens.tokComma)
                    {
                        GetNextSymbol();
                        Condition();
                        if (sym.Token == Symbol.Tokens.tokRightParent)
                        {
                            GetNextSymbol();
                            code!.Add(opcode);
                        }
                        else
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' between function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' between function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function name", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void CallQuaternaryFunction(Opcodes opcode)
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();
                Condition();
                if (sym.Token == Symbol.Tokens.tokComma)
                {
                    GetNextSymbol();
                    Condition();
                    if (sym.Token == Symbol.Tokens.tokComma)
                    {
                        GetNextSymbol();
                        Condition();
                        if (sym.Token == Symbol.Tokens.tokComma)
                        {
                            GetNextSymbol();
                            Condition();
                            if (sym.Token == Symbol.Tokens.tokRightParent)
                            {
                                GetNextSymbol();
                                code!.Add(opcode);
                            }
                            else
                                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                        }
                        else
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' between function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' between function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' between function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function name", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void CallRnd()
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();
                if (sym.Token == Symbol.Tokens.tokRightParent)
                {
                    GetNextSymbol();
                    code!.Add(Opcodes.Rnd);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' - Rnd() takes no parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after Rnd", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void CallRoundFunction()
        {
            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();
                Condition();
                if (sym.Token == Symbol.Tokens.tokComma)
                {
                    GetNextSymbol();
                    Condition();
                }
                else
                {
                    code!.Add(Opcodes.PushValue, 0);
                }
                if (sym.Token == Symbol.Tokens.tokRightParent)
                {
                    GetNextSymbol();
                    code!.Add(Opcodes.Round);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function name", sym.Line, sym.Col, sym.Index, sym.Text);
        }
    }
}
