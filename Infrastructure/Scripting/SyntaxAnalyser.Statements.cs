// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Scripting
{
    public partial class SyntaxAnalyser
    {
        private void StatementList(bool singleLineOnly, bool allowFunctionDeclarations, int exitsAllowed, params Symbol.Tokens[] endSymbols)
        {
            do
            {
                while (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                {
                    if (sym.Text == "\n" & singleLineOnly) { return; }
                    GetNextSymbol();
                }

                if (endSymbols.Contains(sym.Token)) { return; }

                Statement(singleLineOnly, allowFunctionDeclarations, exitsAllowed);
            }
            while (code!.ErrorObject!.Number == 0);
        }

        private void Statement(bool singleLineOnly, bool allowFunctionDeclarations, int exitsAllowed)
        {
            string Ident;

            switch (sym.Token)
            {
                case Symbol.Tokens.tokExternal:
                    if (allowExternal)
                    {
                        GetNextSymbol();
                        VariableDeclaration(true);
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "IMPORT declarations not allowed", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokConst:
                    GetNextSymbol();
                    ConstDeclaration();
                    break;

                case Symbol.Tokens.tokDim:
                    GetNextSymbol();
                    VariableDeclaration(false);
                    break;

                case Symbol.Tokens.tokFunction:
                    if (allowFunctionDeclarations)
                        FunctionDefinition();
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "No function declarations allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokSub:
                    if (allowFunctionDeclarations)
                        FunctionDefinition();
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "No function declarations allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokIf:
                    GetNextSymbol();
                    IFStatement(singleLineOnly, exitsAllowed);
                    break;

                case Symbol.Tokens.tokFor:
                    GetNextSymbol();
                    FORStatement(singleLineOnly, exitsAllowed);
                    break;

                case Symbol.Tokens.tokDo:
                    GetNextSymbol();
                    DoStatement(singleLineOnly, exitsAllowed);
                    break;

                case Symbol.Tokens.tokSelect:
                    GetNextSymbol();
                    SelectCaseStatement(singleLineOnly, exitsAllowed);
                    break;

                case Symbol.Tokens.tokExit:
                    GetNextSymbol();
                    HandleExitStatement(exitsAllowed);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokDebugPrint:
                    GetNextSymbol();
                    if (errorObject!.Number == 0)
                    {
                        ActualOptionalParameter("");
                        code!.Add(Opcodes.DebugPrint);
                    }
                    break;

                case Symbol.Tokens.tokDebugClear:
                    code!.Add(Opcodes.DebugClear);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokDebugShow:
                    code!.Add(Opcodes.DebugShow);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokDebugHide:
                    code!.Add(Opcodes.DebugHide);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokOutput:
                    GetNextSymbol();
                    CallMsg(true);
                    break;

                case Symbol.Tokens.tokMsgbox:
                    GetNextSymbol();
                    CallMsgBox(true);
                    break;

                case Symbol.Tokens.tokDoEvents:
                    code!.Add(Opcodes.DoEvents);
                    GetNextSymbol();
                    break;

                case Symbol.Tokens.tokInputbox:
                    GetNextSymbol();
                    CallInputbox(true);
                    break;

                case Symbol.Tokens.tokIdentifier:
                    Ident = sym.Text;
                    GetNextSymbol();

                    switch (sym.Token)
                    {
                        case Symbol.Tokens.tokEq:
                        case Symbol.Tokens.tokPlusEq:
                        case Symbol.Tokens.tokMinusEq:
                        case Symbol.Tokens.tokMultiplicationEq:
                        case Symbol.Tokens.tokDivisionEq:
                        case Symbol.Tokens.tokStringConcatEq:
                        case Symbol.Tokens.tokDivEq:
                        case Symbol.Tokens.tokModEq:
                            StatementComparativeOperators(Ident);
                            break;

                        default:
                            CallUserdefinedFunction(Ident);
                            break;
                    }
                    break;

                default:
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: declaration, function call or assignment", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;
            }
        }

        private void HandleExitStatement(int exitsAllowed)
        {
            switch (sym.Token)
            {
                case Symbol.Tokens.tokDo:
                    if (exitsAllowed + Exits.ExitDo == Exits.ExitDo)
                        code!.Add(Opcodes.JumpPop);
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT DO' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokFor:
                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitFor)) == Convert.ToByte(Exits.ExitFor))
                        code!.Add(Opcodes.JumpPop);
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT FOR' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokSub:
                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitSub)) == Convert.ToByte(Exits.ExitSub))
                        code!.Add(Opcodes.Return);
                    else if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitFunction)) == Convert.ToByte(Exits.ExitFunction))
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'EXIT FUNCTION' in function", sym.Line, sym.Col, sym.Index, sym.Text);
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT SUB' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                case Symbol.Tokens.tokFunction:
                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitFunction)) == Convert.ToByte(Exits.ExitFunction))
                        code!.Add(Opcodes.Return);
                    else if ((Convert.ToByte(exitsAllowed) & +Convert.ToByte(Exits.ExitSub)) == Convert.ToByte(Exits.ExitSub))
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'EXIT SUB' in sub", sym.Line, sym.Col, sym.Index, sym.Text);
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT FUNCTION' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;

                default:
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'DO' or 'FOR' or 'FUNCTION' after 'EXIT'", sym.Line, sym.Col, sym.Index, sym.Text);
                    break;
            }
        }

        private void StatementComparativeOperators(string Ident)
        {
            var op = sym.Token;

            if (symboltable!.Exists(Ident, null, Identifier.IdentifierTypes.IdConst))
            {
                errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists,
                  "Statement",
                  "Assignment to constant " + Ident + " not allowed",
                  sym.Line, sym.Col, sym.Index, Ident);
            }

            if (this.optionExplicit && !symboltable.Exists(Ident, null, Identifier.IdentifierTypes.IdIsVariableOfFunction))
            {
                errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists,
                  "Statement",
                  "Variable/Function " + Ident + " not declared",
                   sym.Line, sym.Col, sym.Index, Ident);
            }

            if (op != Symbol.Tokens.tokEq)
            {
                this.code!.Add(Opcodes.PushVariable, Ident);
            }

            GetNextSymbol();
            Condition();

            switch (op)
            {
                case Symbol.Tokens.tokPlusEq:
                    code!.Add(Opcodes.Add);
                    break;

                case Symbol.Tokens.tokMinusEq:
                    code!.Add(Opcodes.Sub);
                    break;

                case Symbol.Tokens.tokMultiplicationEq:
                    code!.Add(Opcodes.Multiplication);
                    break;

                case Symbol.Tokens.tokDivisionEq:
                    code!.Add(Opcodes.Division);
                    break;

                case Symbol.Tokens.tokStringConcatEq:
                    code!.Add(Opcodes.StringConcat);
                    break;

                case Symbol.Tokens.tokDivEq:
                    code!.Add(Opcodes.Div);
                    break;

                case Symbol.Tokens.tokModEq:
                    code!.Add(Opcodes.Mod);
                    break;
            }

            code!.Add(Opcodes.Assign, Ident);
        }

        private void ConstDeclaration()
        {
            string ident;
            if (sym.Token == Symbol.Tokens.tokIdentifier)
            {
                if (symboltable!.Exists(sym.Text, true))
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "ConstDeclaration", "Constant identifier '" + sym.Text + "' is already declared", sym.Line, sym.Col, sym.Index, sym.Text);

                ident = sym.Text;
                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokEq)
                {
                    GetNextSymbol();

                    if (sym.Token == Symbol.Tokens.tokNumber | sym.Token == Symbol.Tokens.tokString)
                    {
                        symboltable.Allocate(ident, ScriptValue.FromObject(sym.Value), Identifier.IdentifierTypes.IdConst);
                        code!.Add(Opcodes.AllocConst, ident, sym.Value!);
                        GetNextSymbol();
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.ConstDeclaration", "Expected: const Value", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.ConstDeclaration", "Expected: '=' after const identifier", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.ConstDeclaration", "Expected: const identifier", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void VariableDeclaration(bool external)
        {
            do
            {
                if (sym.Token == Symbol.Tokens.tokIdentifier)
                {
                    if (symboltable!.Exists(sym.Text, true))
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "VariableDeclaration", "Variable identifier '" + sym.Text + "' is already declared", sym.Line, sym.Col, sym.Index, sym.Text);
                    if (external)
                        symboltable.Allocate(sym.Text);
                    else
                    {
                        symboltable.Allocate(sym.Text);
                        code!.Add(Opcodes.AllocVar, sym.Text);
                    }

                    GetNextSymbol();
                    if (sym.Token == Symbol.Tokens.tokComma)
                        GetNextSymbol();
                    else
                        break;
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.VariableDeclaration", "Expected: variable identifier", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            while (true);
        }

        private void FunctionDefinition()
        {
            List<object> formalParameters = new();
            int skipFunctionPC = 0;
            bool isSub = sym.Token == Symbol.Tokens.tokSub;

            void GenerateEndFunctionCode()
            {
                symboltable!.PopScopes(-1);
                code!.Add(Opcodes.Return);
                code.FixUp(skipFunctionPC - 1, code.EndOfCodePc);
                currentFunctionName = null;
            }

            GetNextSymbol();

            if (sym.Token != Symbol.Tokens.tokIdentifier)
            {
                errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Function/Sub Name is missing in definition", sym.Line, sym.Col, sym.Index, sym.Text);
                return;
            }

            string ident = sym.Text;
            GetNextSymbol();

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();

                while (sym.Token == Symbol.Tokens.tokIdentifier)
                {
                    formalParameters.Add(sym.Text);
                    GetNextSymbol();

                    if (sym.Token != Symbol.Tokens.tokComma)
                        break;
                    GetNextSymbol();
                }

                if (sym.Token == Symbol.Tokens.tokRightParent)
                    GetNextSymbol();
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: ',' or ')' or identifier", sym.Line, sym.Col, sym.Index, sym.Text);
            }

            var definition = symboltable!.Allocate(ident, null, isSub ? Identifier.IdentifierTypes.IdSub : Identifier.IdentifierTypes.IdFunction);
            code!.Add(Opcodes.AllocVar, ident);

            skipFunctionPC = code!.Add(Opcodes.Jump);
            definition.Address = code.EndOfCodePc;

            symboltable.PushScope();

            definition.FormalParameters = formalParameters;
            for (int i = 0; i < formalParameters.Count; i++)
                symboltable.Allocate(formalParameters[i].ToString()!, null);

            currentFunctionName = isSub ? null : ident;

            StatementList(false, true, isSub ? (int)Exits.ExitSub : (int)Exits.ExitFunction, Symbol.Tokens.tokEof, Symbol.Tokens.tokEnd, Symbol.Tokens.tokEndfunction, Symbol.Tokens.tokEndsub);

            if (sym.Token == Symbol.Tokens.tokEndfunction || sym.Token == Symbol.Tokens.tokEndsub)
            {
                if (isSub && sym.Token != Symbol.Tokens.tokEndsub)
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END SUB' or 'ENDSUB' at end of sub body", sym.Line, sym.Col, sym.Index, sym.Text);

                GetNextSymbol();
                GenerateEndFunctionCode();
                return;
            }

            if (sym.Token == Symbol.Tokens.tokEnd)
            {
                GetNextSymbol();

                if (isSub && sym.Token == Symbol.Tokens.tokSub)
                {
                    GetNextSymbol();
                    GenerateEndFunctionCode();
                    return;
                }

                if (!isSub && sym.Token == Symbol.Tokens.tokFunction)
                {
                    GetNextSymbol();
                    GenerateEndFunctionCode();
                    return;
                }

                if (isSub)
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END SUB' or 'ENDSUB' at end of sub body", sym.Line, sym.Col, sym.Index, sym.Text);
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END FUNCTION' or 'ENDFUNCTION' at end of function body", sym.Line, sym.Col, sym.Index, sym.Text);
                return;
            }

            errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END FUNCTION'/'ENDFUNCTION', 'END SUB'/'ENDSUB' at end of function body", sym.Line, sym.Col, sym.Index, sym.Text);
        }

        private void CallUserdefinedFunction(string ident)
        {
            bool requireRightParent = false;

            if (!symboltable!.Exists(ident, null, Identifier.IdentifierTypes.IdSubOfFunction))
            {
                errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "Statement", "Function/Sub '" + ident + "' not declared", sym.Line, sym.Col, sym.Index, ident);
            }

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                requireRightParent = true;
                GetNextSymbol();
            }

            Identifier? definition;
            definition = symboltable.Retrieve(ident);
            if (definition == null)
            {
                return;
            }

            code!.Add(Opcodes.PushScope);

            int n = 0;
            int i = 0;

            if (sym.Token == Symbol.Tokens.tokStatementDelimiter | sym.Token == Symbol.Tokens.tokEof)
                n = 0;
            else
                do
                {
                    if (n > definition.FormalParameters!.Count - 1) { break; }
                    if (n > 0) { GetNextSymbol(); }

                    Condition();

                    n++;
                }
                while (sym.Token == Symbol.Tokens.tokComma);

            if (definition.FormalParameters!.Count != n)
                errorObject!.Raise((int)InterpreterError.ParsErrors.errWrongNumberOfParams, "SyntaxAnalyser.Statement", "Wrong number of parameters in call to function '" + ident + "' (" + definition.FormalParameters.Count + " expected but " + n + " found)", sym.Line, sym.Col, sym.Index, sym.Text);

            for (i = definition.FormalParameters.Count - 1; i >= 0; i += -1)
            {
                code.Add(Opcodes.AllocVar, definition.FormalParameters[i]);
                code.Add(Opcodes.Assign, definition.FormalParameters[i]);
            }

            if (requireRightParent)
            {
                if (sym.Token == Symbol.Tokens.tokRightParent)
                    GetNextSymbol();
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }

            code.Add(Opcodes.Call, definition.Address);

            code.Add(Opcodes.PopScope);
        }
    }
}
