// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Control-Flow-Statements des SyntaxAnalysers: IF, FOR, DO, SELECT CASE.
/// </summary>
namespace Klacks.Api.Infrastructure.Scripting
{
    public partial class SyntaxAnalyser
    {
        // IFStatement ::= "IF" Condition "THEN" Statementlist [ "ELSE" Statementlist ] [ "END IF" ]
        private void IFStatement(bool singleLineOnly, int exitsAllowed)
        {
            Condition();

            int thenPC = 0;
            int elsePC = 0;
            if (sym.Token != Symbol.Tokens.tokThen)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "THEN missing after IF", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();

            bool thisIFisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
            if (sym.Token == Symbol.Tokens.tokStatementDelimiter) { GetNextSymbol(); }

            singleLineOnly = singleLineOnly || thisIFisSingleLineOnly;

            thenPC = code!.Add(Opcodes.JumpFalse);

            StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEof, Symbol.Tokens.tokElse, Symbol.Tokens.tokEnd, Symbol.Tokens.tokEndif);

            elsePC = HandleElseBlock(singleLineOnly, exitsAllowed, thenPC);

            HandleEndIfBlock(thisIFisSingleLineOnly);

            if (elsePC == 0)
                code!.FixUp(thenPC - 1, code.EndOfCodePc);
            else
                code!.FixUp(elsePC - 1, code.EndOfCodePc);
        }

        private int HandleElseBlock(bool singleLineOnly, int exitsAllowed, int thenPC)
        {
            if (sym.Token != Symbol.Tokens.tokElse)
                return 0;

            int elsePC = code!.Add(Opcodes.Jump);
            code.FixUp(thenPC - 1, elsePC);

            GetNextSymbol();

            StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEof, Symbol.Tokens.tokEnd, Symbol.Tokens.tokEndif);

            return elsePC;
        }

        private void HandleEndIfBlock(bool thisIFisSingleLineOnly)
        {
            if (sym.Token == Symbol.Tokens.tokEnd)
            {
                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokIf)
                    GetNextSymbol();
                else
                    errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "'END IF' or 'ENDIF' expected to close IF-statement", sym.Line, sym.Col, sym.Index, sym.Text));
            }
            else if (sym.Token == Symbol.Tokens.tokEndif)
                GetNextSymbol();
            else if (!thisIFisSingleLineOnly)
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "'END IF' or 'ENDIF' expected to close IF-statement", sym.Line, sym.Col, sym.Index, sym.Text));
        }

        // FORStatement ::= "FOR" variable "=" Value "TO" [ "STEP" Value ] Value Statementlist "NEXT"
        private void FORStatement(bool singleLineOnly, int exitsAllowed)
        {
            if (sym.Token != Symbol.Tokens.tokIdentifier)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Counter variable missing in FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            if (optionExplicit & !symboltable!.Exists(sym.Text, null, Identifier.IdentifierTypes.IdVariable))
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.FORStatement", "Variable '" + sym.Text + "' not declared", sym.Line, sym.Col, sym.Index, sym.Text));

            string counterVariable = sym.Text;

            GetNextSymbol();

            if (sym.Token != Symbol.Tokens.tokEq)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: '=' after counter variable", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();

            Condition();

            code!.Add(Opcodes.Assign, counterVariable);

            if (sym.Token != Symbol.Tokens.tokTo)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: 'TO' after start Value of FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();

            Condition();

            ParseForStepValue();

            int pushExitAddrPC = code!.Add(Opcodes.PushValue);

            int forPC = code.EndOfCodePc;

            bool thisFORisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
            if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                GetNextSymbol();

            singleLineOnly |= thisFORisSingleLineOnly;

            StatementList(singleLineOnly, false, Convert.ToByte(Exits.ExitFor) | Convert.ToByte(exitsAllowed), Symbol.Tokens.tokEof, Symbol.Tokens.tokNext);

            if (sym.Token == Symbol.Tokens.tokNext)
                GetNextSymbol();
            else if (!thisFORisSingleLineOnly)
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: 'NEXT' at end of FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text));

            EmitForLoopEpilogue(counterVariable, forPC, pushExitAddrPC);
        }

        private void ParseForStepValue()
        {
            if (sym.Token == Symbol.Tokens.tokStep)
            {
                GetNextSymbol();
                Condition();
            }
            else
                code!.Add(Opcodes.PushValue, 1);
        }

        private void EmitForLoopEpilogue(string counterVariable, int forPC, int pushExitAddrPC)
        {
            code!.Add(Opcodes.PopWithIndex, 1);
            code.Add(Opcodes.PushVariable, counterVariable);
            code.Add(Opcodes.Add);
            code.Add(Opcodes.Assign, counterVariable);

            code.Add(Opcodes.PopWithIndex, 2);
            code.Add(Opcodes.PushVariable, counterVariable);
            code.Add(Opcodes.GEq);
            code.Add(Opcodes.JumpTrue, forPC);

            code.Add(Opcodes.Pop);
            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc);
            code.Add(Opcodes.Pop);
            code.Add(Opcodes.Pop);
        }

        // DoStatement ::= "DO" [ "WHILE" Condition ] Statementlist "LOOP" [ ("UNTIL" | "WHILE") Condition ) ]
        private void DoStatement(bool singleLineOnly, int exitsAllowed)
        {
            int conditionPC = 0;
            bool doWhile = false;

            int pushExitAddrPC = code!.Add(Opcodes.PushValue);

            int doPC = code.EndOfCodePc;

            if (sym.Token == Symbol.Tokens.tokWhile)
            {
                doWhile = true;
                GetNextSymbol();

                Condition();

                conditionPC = code.Add(Opcodes.JumpFalse);
            }

            bool thisDOisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
            if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                GetNextSymbol();

            singleLineOnly = singleLineOnly | thisDOisSingleLineOnly;

            StatementList(singleLineOnly, false, Convert.ToByte(Exits.ExitDo) | Convert.ToByte(exitsAllowed), Symbol.Tokens.tokEof, Symbol.Tokens.tokLoop);

            if (sym.Token == Symbol.Tokens.tokLoop)
            {
                GetNextSymbol();
                HandleLoopCondition(doWhile, doPC, conditionPC, pushExitAddrPC);
            }
            else if (!(doWhile & thisDOisSingleLineOnly))
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.DoStatement", "'LOOP' is missing at end of DO-statement", sym.Line, sym.Col, sym.Index));
        }

        private void HandleLoopCondition(bool doWhile, int doPC, int conditionPC, int pushExitAddrPC)
        {
            if (sym.Token == Symbol.Tokens.tokWhile || sym.Token == Symbol.Tokens.tokUntil)
            {
                if (doWhile == true)
                    errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.DoStatement", "No 'WHILE'/'UNTIL' allowed after 'LOOP' in DO-WHILE-statement", sym.Line, sym.Col, sym.Index));

                bool loopWhile = sym.Token == Symbol.Tokens.tokWhile;

                GetNextSymbol();

                Condition();

                code!.Add(loopWhile == true ? Opcodes.JumpTrue : Opcodes.JumpFalse, doPC);

                code.Add(Opcodes.Pop);

                code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc);
                return;
            }

            code!.Add(Opcodes.Jump, doPC);
            if (doWhile == true)
                code.FixUp(conditionPC - 1, code.EndOfCodePc);

            code.Add(Opcodes.Pop);

            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc);
        }

        // SelectCaseStatement ::= "SELECT" "CASE" Expression { "CASE" ( CaseList | "ELSE" ) StatementList } "END" "SELECT"
        private void SelectCaseStatement(bool singleLineOnly, int exitsAllowed)
        {
            if (sym.Token != Symbol.Tokens.tokCase)
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.SelectCaseStatement", "Expected 'CASE' after 'SELECT'", sym.Line, sym.Col, sym.Index, sym.Text));
                return;
            }

            GetNextSymbol();

            Condition();

            if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                GetNextSymbol();

            var endJumps = new List<int>();

            while (sym.Token == Symbol.Tokens.tokCase && errorObject!.Number == 0)
            {
                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokElse)
                {
                    GetNextSymbol();

                    if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                        GetNextSymbol();

                    StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEof, Symbol.Tokens.tokEnd, Symbol.Tokens.tokCase);
                }
                else
                {
                    int nextCaseJump = ParseCaseExpressionList();

                    if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                        GetNextSymbol();

                    StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEof, Symbol.Tokens.tokEnd, Symbol.Tokens.tokCase);

                    endJumps.Add(code!.Add(Opcodes.Jump));

                    code.FixUp(nextCaseJump - 1, code.EndOfCodePc);
                }
            }

            HandleEndSelect();

            foreach (var jumpPc in endJumps)
            {
                code!.FixUp(jumpPc - 1, code.EndOfCodePc);
            }

            code!.Add(Opcodes.Pop);
        }

        private int ParseCaseExpressionList()
        {
            bool firstValue = true;
            int matchJump = -1;

            do
            {
                if (!firstValue)
                    GetNextSymbol();
                firstValue = false;

                code!.Add(Opcodes.PopWithIndex, 0);

                Condition();

                code.Add(Opcodes.Eq);

                if (sym.Token == Symbol.Tokens.tokComma)
                {
                    int tempJump = code.Add(Opcodes.JumpTrue);
                    if (matchJump == -1)
                        matchJump = tempJump;
                    else
                    {
                        code.FixUp(matchJump - 1, code.EndOfCodePc);
                        matchJump = tempJump;
                    }
                }
            }
            while (sym.Token == Symbol.Tokens.tokComma && errorObject!.Number == 0);

            int nextCaseJump = code!.Add(Opcodes.JumpFalse);

            if (matchJump != -1)
                code.FixUp(matchJump - 1, code.EndOfCodePc);

            return nextCaseJump;
        }

        private void HandleEndSelect()
        {
            if (sym.Token == Symbol.Tokens.tokEnd)
            {
                GetNextSymbol();
                if (sym.Token == Symbol.Tokens.tokSelect)
                    GetNextSymbol();
                else
                    errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.SelectCaseStatement", "Expected 'SELECT' after 'END'", sym.Line, sym.Col, sym.Index, sym.Text));
            }
            else
            {
                errorObject!.Raise(new InterpreterErrorInfo((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.SelectCaseStatement", "Expected 'END SELECT' to close SELECT CASE statement", sym.Line, sym.Col, sym.Index, sym.Text));
            }
        }
    }
}
