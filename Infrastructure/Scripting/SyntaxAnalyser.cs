namespace Klacks.Api.Infrastructure.Scripting
{
    public partial class SyntaxAnalyser
    {
        private bool allowExternal;
        private Code? code;
        private InterpreterError? errorObject;
        private LexicalAnalyser lex = new LexicalAnalyser();
        private bool optionExplicit;
        private Symbol sym = new();
        private Scopes? symboltable;

        public SyntaxAnalyser(InterpreterError interpreterError)
        {
            errorObject = interpreterError;
        }

        private enum Exits
        {
            ExitNone = 0,
            ExitDo = 1,
            ExitFor = 2,
            ExitFunction = 4,
            ExitSub = 8,
        }

        public Code Parse(IInputStream source, Code code, bool optionExplicit = true, bool allowExternal = true)
        {
            lex.Connect(source, code.ErrorObject!);

            symboltable = new Scopes();
            symboltable.PushScope();

            errorObject = code.ErrorObject!;
            this.code = code;
            this.optionExplicit = optionExplicit;
            this.allowExternal = allowExternal;

            GetNextSymbol();

            StatementList(false, true, (int)Exits.ExitNone, Symbol.Tokens.tokEof);

            if (errorObject.Number != 0)
            {
                return this.code;
            }

            if (!sym.Token.Equals(Symbol.Tokens.tokEof))
            {
                errorObject.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Parse", "Expected: end of statement", sym.Line, sym.Col, sym.Index, sym.Text);
            }

            return this.code;
        }

        private Symbol GetNextSymbol()
        {
            sym = lex.GetNextSymbol();
            return sym;
        }

        private bool InSymbolSet(Symbol.Tokens token, params Symbol.Tokens[] tokenSet)
        {
            return tokenSet.Contains(token);
        }

        private void ActualOptionalParameter(object default_Renamed)
        {
            if (sym.Token == Symbol.Tokens.tokComma | sym.Token == Symbol.Tokens.tokStatementDelimiter | sym.Token == Symbol.Tokens.tokEof | sym.Token == Symbol.Tokens.tokRightParent)
            {
                code!.Add(Opcodes.PushValue, default_Renamed);
            }
            else
            {
                Condition();
            }

            if (sym.Token == Symbol.Tokens.tokComma)
            {
                GetNextSymbol();
            }
        }
    }
}
