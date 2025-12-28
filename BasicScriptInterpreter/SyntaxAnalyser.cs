namespace Klacks.Api.BasicScriptInterpreter
{
    /// <summary>
    /// SyntaxAnalyser: Führt die Syntaxanalyser durch, erzeugt
    /// aber auch den Code. Der Parser steuert den ganzen Übersetzungsprozess.
    /// </summary>
    public class SyntaxAnalyser
    {
        private bool allowExternal; // sollen EXTERNAL-Definitionen von Variablen erlaubt sein?
        private Code? code;
        private InterpreterError? errorObject;
        private LexicalAnalyser lex = new LexicalAnalyser();
        private bool optionExplicit; // müssen alle Identifier vor Benutzung explizit deklariert werden?
        private Symbol sym = new();  // das jeweils aktuelle Symbol

        private Scopes? symboltable;  // Symboltabelle während der Übersetzungszeit. Sie vermerkt, welche Identifier mit welchem Typ usw. bereits definiert sind und simuliert

        public SyntaxAnalyser(InterpreterError interpreterError)
        {
            errorObject = interpreterError;
        }

        ~SyntaxAnalyser()
        {
            Dispose();
        }

        private enum Exits
        {
            ExitNone = 0,
            ExitDo = 1,
            ExitFor = 2,
            ExitFunction = 4,
            ExitSub = 8,
        }

        public void Dispose()
        {
            errorObject = null;
        }

        public Code Parse(IInputStream source, Code code, bool optionExplicit = true, bool allowExternal = true)
        {
            lex.Connect(source, code.ErrorObject!);

            symboltable = new Scopes();  // globalen und ersten Gültigkeitsbereich anlegen
            symboltable.PushScope();

            errorObject = code.ErrorObject!;
            this.code = code;
            this.optionExplicit = optionExplicit;
            this.allowExternal = allowExternal;

            // --- Erstes Symbol lesen
            GetNextSymbol();

            // --- Wurzelregel der Grammatik aufrufen und Syntaxanalyse starten
            StatementList(false, true, (int)Exits.ExitNone, Symbol.Tokens.tokEof);

            if (errorObject.Number != 0)
            {
                return this.code;
            }

            // Wenn wir hier ankommen, dann muss der komplette Code korrekt erkannt
            // worden sein, d.h. es sind alle Symbole gelesen. Falls also nicht
            // das Eof-Symbol aktuell ist, ist ein Symbol nicht erkannt worden und
            // ein Fehler liegt vor.
            if (!sym.Token.Equals(Symbol.Tokens.tokEof))
            {
                errorObject.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Parse", "Expected: end of statement", sym.Line, sym.Col, sym.Index, sym.Text);
            }

            return this.code;
        }

        private void ActualOptionalParameter(object default_Renamed)
        {
            if (sym.Token == Symbol.Tokens.tokComma | sym.Token == Symbol.Tokens.tokStatementDelimiter | sym.Token == Symbol.Tokens.tokEof | sym.Token == Symbol.Tokens.tokRightParent)
            {
                // statt eines Parameters sind wir nur auf ein "," oder das Statement-Ende gestossen
                // wir nehmen daher den default-Wert an
                code!.Add(Opcodes.PushValue, default_Renamed);
            }
            else
            {
                // Parameterwert bestimmen
                Condition();
            }

            if (sym.Token == Symbol.Tokens.tokComma)
            {
                GetNextSymbol();
            }
        }

        private Symbol.Tokens Boxes(Symbol.Tokens operator_Renamed)
        {
            GetNextSymbol();

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();

                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokMsgbox:
                        {
                            CallMsgBox(false);
                            break;
                        }

                    case Symbol.Tokens.tokInputbox:
                        {
                            CallInputbox(false);
                            break;
                        }

                    case Symbol.Tokens.tokMessage:
                        {
                            CallMsg(false);
                            break;
                        }
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

        /// <summary>
        /// Inputbox aufrufen
        /// </summary>
        private void CallInputbox(bool dropReturnValue)
        {
            ActualOptionalParameter(string.Empty); // Prompt
            ActualOptionalParameter(string.Empty); // Title
            ActualOptionalParameter(string.Empty); // Default
                                                   //ActualOptionalParameter(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / (double)5);  // xPos
                                                   //ActualOptionalParameter(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / (double)5);  // yPos

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

        /// <summary>
        /// Benutzerdefinierte Funktion aufrufen: feststellen, ob für alle formalen Parameter ein aktueller Param. angegeben ist.
        /// </summary>
        private void CallUserdefinedFunction(string ident)
        {
            bool requireRightParent = false;

            // Identifier überhaupt als Funktion definiert?
            if (!symboltable!.Exists(ident, null, Identifier.IdentifierTypes.IdSubOfFunction))
            {
                errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "Statement", "Function/Sub '" + ident + "' not declared", sym.Line, sym.Col, sym.Index, ident);
            }

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                requireRightParent = true;
                GetNextSymbol();
            }

            // Funktionsdefinition laden (Parameterzahl, Adresse)
            Identifier definition;
            definition = symboltable.Retrieve(ident)!;

            // --- Function-Scope vorbereiten

            // Neuen Scope für die Funktion öffnen
            code!.Add(Opcodes.PushScope);

            // --- Parameter verarbeiten
            int n = 0;
            int i = 0;

            if (sym.Token == Symbol.Tokens.tokStatementDelimiter | sym.Token == Symbol.Tokens.tokEof)
                // Aufruf ohne Parameter
                n = 0;
            else
                // Funktion mit Parametern: Parameter { "," Parameter }
                do
                {
                    if (n > definition.FormalParameters!.Count - 1) { break; }
                    if (n > 0) { GetNextSymbol(); }

                    Condition();

                    n++;
                }
                // wir standen noch auf dem "," nach dem vorhergehenden Parameter // Wert des n-ten Parameters auf den Stack legen
                while (sym.Token == Symbol.Tokens.tokComma);

            // Wurde die richtige Anzahl Parameter übergeben?
            if (definition.FormalParameters!.Count != n)
                errorObject!.Raise((int)InterpreterError.ParsErrors.errWrongNumberOfParams, "SyntaxAnalyser.Statement", "Wrong number of parameters in call to function '" + ident + "' (" + definition.FormalParameters.Count + " expected but " + n + " found)", sym.Line, sym.Col, sym.Index, sym.Text);

            // Formale Parameter als lokale Variablen der Funktion definieren und zuweisen
            // (in umgekehrter Reihenfolge, weil die Werte so auf dem Stack liegen)
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

            // --- Funktion rufen
            code.Add(Opcodes.Call, definition.Address);

            // --- Scopes aufräumen
            code.Add(Opcodes.PopScope);
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
                            {
                                code!.Add(Opcodes.Sin);
                                break;
                            }

                        case Symbol.Tokens.tokCos:
                            {
                                code!.Add(Opcodes.Cos);
                                break;
                            }

                        case Symbol.Tokens.tokTan:
                            {
                                code!.Add(Opcodes.Tan);
                                break;
                            }

                        case Symbol.Tokens.tokATan:
                            {
                                code!.Add(Opcodes.ATan);
                                break;
                            }
                    }
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameter", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function Name", sym.Line, sym.Col, sym.Index, sym.Text);

            return operator_Renamed;
        }

        // Einstieg für die Auswertung math. Ausdrücke (s.a. Expression())
        private void Condition()
        {
            // ConditionalTerm { "OR" ConditionalTerm }
            ConditionalTerm();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokOr))
            {
                GetNextSymbol();
                ConditionalTerm();

                code!.Add(Opcodes.Or);
            }
        }

        private void ConditionalFactor()
        {
            // Expression { ( "=" | "<>" | "<=" | "<" | ">=" | ">" ) Expression }
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
                        {
                            code!.Add(Opcodes.Eq);
                            break;
                        }

                    case Symbol.Tokens.tokNotEq:
                        {
                            code!.Add(Opcodes.NotEq);
                            break;
                        }

                    case Symbol.Tokens.tokLEq:
                        {
                            code!.Add(Opcodes.LEq);
                            break;
                        }

                    case Symbol.Tokens.tokLt:
                        {
                            code!.Add(Opcodes.Lt);
                            break;
                        }

                    case Symbol.Tokens.tokGEq:
                        {
                            code!.Add(Opcodes.GEq);
                            break;
                        }

                    case Symbol.Tokens.tokGt:
                        {
                            code!.Add(Opcodes.Gt);
                            break;
                        }
                }
            }
        }

        // Bei AND-Verknüpfungen werden nur soviele Operanden tatsächlich berechnet, bis einer FALSE ergibt! In diesem Fall wird das
        // Ergebnis aller AND-Verknüpfungen sofort auch auf FALSE gesetzt und die restlichen Prüfungen/Kalkulationen nicht mehr durchgeführt.
        private void ConditionalTerm()
        {
            var operandPCs = new List<object>();

            ConditionalFactor();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokAnd))
            {
                operandPCs.Add(code!.Add(Opcodes.JumpFalse));

                GetNextSymbol();
                ConditionalFactor();
            }

            int thenPC;
            if (operandPCs.Count() > 0)
            {
                operandPCs.Add(code!.Add(Opcodes.JumpFalse));

                // wenn wir hier ankommen, dann sind alle AND-Operanden TRUE
                code.Add(Opcodes.PushValue, true); // also dieses Ergebnis auch auf den Stack legen
                thenPC = code.Add(Opcodes.Jump); // und zum Code springen, der mit dem Ergebnis weiterarbeitet

                // wenn wir hier ankommen, dann war mindestens ein
                // AND-Operand FALSE
                // Alle Sprünge von Operandentests hierher umleiten, da
                // sie ja FALSE ergeben haben
                for (int i = 0; i < operandPCs.Count(); i++)
                    code.FixUp(Convert.ToInt32(operandPCs[i]) - 1, code.EndOfCodePc);
                code.Add(Opcodes.PushValue, false); // also dieses Ergebnis auch auf den Stack legen

                code.FixUp(thenPC - 1, code.EndOfCodePc);
            }
        }

        private void ConstDeclaration()
        {
            string ident;
            if (sym.Token == Symbol.Tokens.tokIdentifier)
            {
                // Wurde Identifier schon für etwas anderes in diesem Scope benutzt?
                if (symboltable!.Exists(sym.Text, true))
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "ConstDeclaration", "Constant identifier '" + sym.Text + "' is already declared", sym.Line, sym.Col, sym.Index, sym.Text);

                ident = sym.Text;

                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokEq)
                {
                    GetNextSymbol();

                    if (sym.Token == Symbol.Tokens.tokNumber | sym.Token == Symbol.Tokens.tokString)
                    {
                        symboltable.Allocate(ident, sym.Value, Identifier.IdentifierTypes.IdConst);
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

        // DoStatement ::= "DO" [ "WHILE" Condition ] Statementlist "LOOP" [ ("UNTIL" | "WHILE") Condition ) ]
        private void DoStatement(bool singleLineOnly, int exitsAllowed)
        {
            int conditionPC = 0;
            int doPC;
            int pushExitAddrPC;
            bool thisDOisSingleLineOnly;
            bool doWhile = false;

            // EXIT-Adresse auf den Stack legen

            pushExitAddrPC = code!.Add(Opcodes.PushValue);

            doPC = code.EndOfCodePc;

            if (sym.Token == Symbol.Tokens.tokWhile)
            {
                // DO-WHILE
                doWhile = true;
                GetNextSymbol();

                Condition();

                conditionPC = code.Add(Opcodes.JumpFalse);
            }

            thisDOisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
            if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                GetNextSymbol();

            singleLineOnly = singleLineOnly | thisDOisSingleLineOnly;

            // DO-body
            StatementList(singleLineOnly, false, Convert.ToByte(Exits.ExitDo) | Convert.ToByte(exitsAllowed), Symbol.Tokens.tokEof, Symbol.Tokens.tokLoop);

            bool loopWhile;
            if (sym.Token == Symbol.Tokens.tokLoop)
            {
                GetNextSymbol();

                switch (sym.Token)
                {
                    case Symbol.Tokens.tokWhile:
                        {
                            if (doWhile == true)
                                errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.DoStatement", "No 'WHILE'/'UNTIL' allowed after 'LOOP' in DO-WHILE-statement", sym.Line, sym.Col, sym.Index);

                            loopWhile = sym.Token == Symbol.Tokens.tokWhile;

                            GetNextSymbol();

                            Condition();

                            code.Add(loopWhile == true ? Opcodes.JumpTrue : Opcodes.JumpFalse, doPC);
                            // Sprung zum Schleifenanf. wenn Bed. entsprechenden Wert hat

                            code.Add(Opcodes.Pop); // Exit-Adresse vom Stack entfernen

                            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc); // Adresse setzen, zu der EXIT springen soll
                            break;
                        }
                    case Symbol.Tokens.tokUntil:
                        {
                            if (doWhile == true)
                                errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.DoStatement", "No 'WHILE'/'UNTIL' allowed after 'LOOP' in DO-WHILE-statement", sym.Line, sym.Col, sym.Index);

                            loopWhile = sym.Token == Symbol.Tokens.tokWhile;

                            GetNextSymbol();

                            Condition();

                            code.Add(loopWhile == true ? Opcodes.JumpTrue : Opcodes.JumpFalse, doPC);
                            // Sprung zum Schleifenanf. wenn Bed. entsprechenden Wert hat

                            code.Add(Opcodes.Pop); // Exit-Adresse vom Stack entfernen

                            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc); // Adresse setzen, zu der EXIT springen soll
                            break;
                        }

                    default:
                        {
                            code.Add(Opcodes.Jump, doPC);
                            if (doWhile == true)
                                code.FixUp(conditionPC - 1, code.EndOfCodePc);

                            code.Add(Opcodes.Pop); // Exit-Adresse vom Stack entfernen, sie wurde ja nicht benutzt

                            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc); // Adresse setzen, zu der EXIT springen soll
                            break;
                        }
                }
            }
            else if (!(doWhile & thisDOisSingleLineOnly))
                errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.DoStatement", "'LOOP' is missing at end of DO-statement", sym.Line, sym.Col, sym.Index);
        }

        private void Expression()
        {
            // Term { ("+" | "-" | "%" | "MOD" | "&") Term }
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
                        {
                            code!.Add(Opcodes.Add);
                            break;
                        }

                    case Symbol.Tokens.tokMinus:
                        {
                            code!.Add(Opcodes.Sub);
                            break;
                        }

                    case Symbol.Tokens.tokMod:
                        {
                            code!.Add(Opcodes.Mod);
                            break;
                        }

                    case Symbol.Tokens.tokStringConcat:
                        {
                            code!.Add(Opcodes.StringConcat);
                            break;
                        }
                }
            }
        }

        private void Factor()
        {
            // Factorial [ "^" Factorial ]
            Factorial();

            if (sym.Token == Symbol.Tokens.tokPower)
            {
                GetNextSymbol();

                Factorial();

                code!.Add(Opcodes.Power);
            }
        }

        // Fakultät
        private void Factorial()
        {
            // Terminal [ "!" ]
            Terminal();

            if (sym.Token == Symbol.Tokens.tokFactorial)
            {
                code!.Add(Opcodes.Factorial);

                GetNextSymbol();
            }
        }

        // FORStatement ::= "FOR" variable "=" Value "TO" [ "STEP" Value ] Value Statementlist "NEXT"
        // (Achtung: bei FOR, DO, IF kann die Anweisung entweder über mehrere Zeilen laufen
        // und muss dann mit einem entsprechenden Endesymbol abgeschlossen sein (NEXT, END IF usw.). Oder
        // sie umfasst nur 1 Zeile und bedarf keines Abschlusses! Daher der Aufwand mit
        // singleLineOnly und thisFORisSingleLineOnly.
        private void FORStatement(bool singleLineOnly, int exitsAllowed)
        {
            string counterVariable = string.Empty;

            if (sym.Token == Symbol.Tokens.tokIdentifier)
            {
                int forPC, pushExitAddrPC;
                bool thisFORisSingleLineOnly;
                if (optionExplicit & !symboltable!.Exists(sym.Text, null, Identifier.IdentifierTypes.IdVariable))
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.FORStatement", "Variable '" + sym.Text + "' not declared", sym.Line, sym.Col, sym.Index, sym.Text);

                counterVariable = sym.Text;

                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokEq)
                {
                    GetNextSymbol();

                    Condition(); // Startwert der FOR-Schleife

                    // Startwert (auf dem Stack) der Zählervariablen zuweisen
                    code!.Add(Opcodes.Assign, counterVariable);

                    if (sym.Token == Symbol.Tokens.tokTo)
                    {
                        GetNextSymbol();

                        Condition(); // Endwert der FOR-Schleife

                        if (sym.Token == Symbol.Tokens.tokStep)
                        {
                            GetNextSymbol();

                            Condition(); // Schrittweite
                        }
                        else
                            // keine explizite Schrittweite, also default auf 1
                            code.Add(Opcodes.PushValue, 1);

                        // EXIT-Adresse auf Stack legen. Es ist wichtig, dass sie zuoberst liegt!
                        // Nur so kommen wir jederzeit an sie mit EXIT heran.
                        pushExitAddrPC = code.Add(Opcodes.PushValue);

                        // hier gehen endlich die Statements innerhalb der Schleife los
                        forPC = code.EndOfCodePc;

                        thisFORisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
                        if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                            GetNextSymbol();

                        singleLineOnly |= thisFORisSingleLineOnly;

                        // FOR-body
                        StatementList(singleLineOnly, false, Convert.ToByte(Exits.ExitFor) | Convert.ToByte(exitsAllowed), Symbol.Tokens.tokEof, Symbol.Tokens.tokNext);

                        if (sym.Token == Symbol.Tokens.tokNext)
                            GetNextSymbol();
                        else if (!thisFORisSingleLineOnly)
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: 'NEXT' at end of FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text);

                        // nach den Schleifenstatements wird die Zählervariable hochgezählt und
                        // geprüft, ob eine weitere Runde gedreht werden soll

                        // Wert der counter variablen um die Schrittweite erhöhen
                        code.Add(Opcodes.PopWithIndex, 1); // Schrittweite
                        code.Add(Opcodes.PushVariable, counterVariable); // aktuellen Zählervariableninhalt holen
                        code.Add(Opcodes.Add); // aktuellen Zähler + Schrittweite
                        code.Add(Opcodes.Assign, counterVariable); // Zählervariable ist jetzt auf dem neuesten Stand

                        // Prüfen, ob Endwert schon überschritten
                        code.Add(Opcodes.PopWithIndex, 2); // Endwert
                        code.Add(Opcodes.PushVariable, counterVariable); // aktuellen Zählervariableninhalt holen (ja, den hatten wir gerade schon mal)
                        code.Add(Opcodes.GEq); // wenn Endwert >= Zählervariable, dann weitermachen
                        code.Add(Opcodes.JumpTrue, forPC);

                        // Stack bereinigen von allen durch FOR darauf zwischengespeicherten Werten
                        code.Add(Opcodes.Pop); // Exit-Adresse vom Stack entfernen
                        code.FixUp(pushExitAddrPC - 1, code.EndOfCodePc); // Adresse setzen, zu der EXIT springen soll
                        code.Add(Opcodes.Pop); // Schrittweite
                        code.Add(Opcodes.Pop); // Endwert
                    }
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: 'TO' after start Value of FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: '=' after counter variable", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Counter variable missing in FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text);
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

        private Symbol GetNextSymbol()
        {
            sym = lex.GetNextSymbol();
            return sym;
        }

        //IFStatement ::= "IF" Condition "THEN" Statementlist [ "ELSE" Statementlist ] [ "END IF" ]
        //("END IF" kann nur entfallen, wenn alle IF-Teile in einer Zeile stehen
        private void IFStatement(bool singleLineOnly, int exitsAllowed)
        {
            bool thisIFisSingleLineOnly;

            Condition();

            int thenPC = 0;
            int elsePC = 0;
            if (sym.Token == Symbol.Tokens.tokThen)
            {
                GetNextSymbol();

                thisIFisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
                if (sym.Token == Symbol.Tokens.tokStatementDelimiter) { GetNextSymbol(); }

                singleLineOnly = singleLineOnly || thisIFisSingleLineOnly;

                thenPC = code!.Add(Opcodes.JumpFalse);
                // Spring zum ELSE-Teil oder ans Ende

                StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEof, Symbol.Tokens.tokElse, Symbol.Tokens.tokEnd, Symbol.Tokens.tokEndif);

                if (sym.Token == Symbol.Tokens.tokElse)
                {
                    elsePC = code.Add(Opcodes.Jump); // Spring ans Ende
                    code.FixUp(thenPC - 1, elsePC);

                    GetNextSymbol();

                    StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEof, Symbol.Tokens.tokEnd, Symbol.Tokens.tokEndif);
                }

                if (sym.Token == Symbol.Tokens.tokEnd)
                {
                    GetNextSymbol();

                    if (sym.Token == Symbol.Tokens.tokIf)
                        GetNextSymbol();
                    else
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "'END IF' or 'ENDIF' expected to close IF-statement", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else if (sym.Token == Symbol.Tokens.tokEndif)
                    GetNextSymbol();
                else if (!thisIFisSingleLineOnly)
                    // kein 'END IF' zu finden ist nur ein Fehler, wenn das IF über mehrere Zeilen geht
                    errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "'END IF' or 'ENDIF' expected to close IF-statement", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "THEN missing after IF", sym.Line, sym.Col, sym.Index, sym.Text);

            if (elsePC == 0)
                code!.FixUp(thenPC - 1, code.EndOfCodePc);
            else
                code!.FixUp(elsePC - 1, code.EndOfCodePc);
        }

        private bool InSymbolSet(Symbol.Tokens token, params Symbol.Tokens[] tokenSet)
        {
            //for (int i = 0; i <= tokenSet.Length - 1; i++)
            //{
            //    if (Token.Equals(tokenSet[i]))
            //    {
            //        return true;

            //    }
            //}

            return tokenSet.Contains(token);
            //return false;
        }

        private void Statement(bool singleLineOnly, bool allowFunctionDeclarations, int exitsAllowed)
        {
            string Ident;

            // Symbol.Tokens op;
            switch (sym.Token)
            {
                case Symbol.Tokens.tokExternal:
                    {
                        if (allowExternal)
                        {
                            GetNextSymbol();

                            VariableDeclaration(true);
                        }
                        else
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "IMPORT declarations not allowed", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokConst:
                    {
                        GetNextSymbol();

                        ConstDeclaration();
                        break;
                    }

                case Symbol.Tokens.tokDim:
                    {
                        GetNextSymbol();

                        VariableDeclaration(false);
                        break;
                    }

                case Symbol.Tokens.tokFunction:
                    {
                        if (allowFunctionDeclarations)
                            FunctionDefinition();
                        else
                            // im aktuellen Kontext (z.B. innerhalb einer FOR-Schleife)
                            // ist eine Funktionsdefinition nicht erlaubt
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "No function declarations allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }
                case Symbol.Tokens.tokSub:
                    {
                        if (allowFunctionDeclarations)
                            FunctionDefinition();
                        else
                            // im aktuellen Kontext (z.B. innerhalb einer FOR-Schleife)
                            // ist eine Funktionsdefinition nicht erlaubt
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "No function declarations allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokIf:
                    {
                        GetNextSymbol();

                        IFStatement(singleLineOnly, exitsAllowed);
                        break;
                    }

                case Symbol.Tokens.tokFor:
                    {
                        GetNextSymbol();

                        FORStatement(singleLineOnly, exitsAllowed);
                        break;
                    }

                case Symbol.Tokens.tokDo:
                    {
                        GetNextSymbol();

                        DoStatement(singleLineOnly, exitsAllowed);
                        break;
                    }

                case Symbol.Tokens.tokExit:
                    {
                        GetNextSymbol();

                        switch (sym.Token)
                        {
                            case Symbol.Tokens.tokDo:
                                {
                                    if (exitsAllowed + Exits.ExitDo == Exits.ExitDo)

                                        code!.Add(Opcodes.JumpPop); // Exit-Adresse liegt auf dem Stack
                                    else
                                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT DO' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            case Symbol.Tokens.tokFor:
                                {
                                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitFor)) == Convert.ToByte(Exits.ExitFor))

                                        code!.Add(Opcodes.JumpPop); // Exit-Adresse liegt auf dem Stack
                                    else
                                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT FOR' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            case Symbol.Tokens.tokSub:
                                {
                                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitSub)) == Convert.ToByte(Exits.ExitSub))

                                        // zum Ende der aktuellen Funktion spring
                                        code!.Add(Opcodes.Return);
                                    else if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitFunction)) == Convert.ToByte(Exits.ExitFunction))
                                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'EXIT FUNCTION' in function", sym.Line, sym.Col, sym.Index, sym.Text);
                                    else
                                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT SUB' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            case Symbol.Tokens.tokFunction:
                                {
                                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.ExitFunction)) == Convert.ToByte(Exits.ExitFunction))

                                        code!.Add(Opcodes.Return);
                                    else if ((Convert.ToByte(exitsAllowed) & +Convert.ToByte(Exits.ExitSub)) == Convert.ToByte(Exits.ExitSub))
                                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'EXIT SUB' in sub", sym.Line, sym.Col, sym.Index, sym.Text);
                                    else
                                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT FUNCTION' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            default:
                                {
                                    errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'DO' or 'FOR' or 'FUNCTION' after 'EXIT'", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }
                        }

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokDebugPrint:
                    {
                        GetNextSymbol();

                        if (errorObject!.Number == 0)
                        {
                            ActualOptionalParameter("");
                            code!.Add(Opcodes.DebugPrint);
                        }

                        break;
                    }

                case Symbol.Tokens.tokDebugClear:
                    {
                        code!.Add(Opcodes.DebugClear);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokDebugShow:
                    {
                        code!.Add(Opcodes.DebugShow);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokDebugHide:
                    {
                        code!.Add(Opcodes.DebugHide);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokMessage:
                    {
                        GetNextSymbol();

                        CallMsg(true);
                        break;
                    }

                case Symbol.Tokens.tokMsgbox:
                    {
                        GetNextSymbol();

                        CallMsgBox(true);
                        break;
                    }

                case Symbol.Tokens.tokDoEvents:
                    {
                        code!.Add(Opcodes.DoEvents);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokInputbox:
                    {
                        GetNextSymbol();

                        CallInputbox(true);
                        break;
                    }

                case Symbol.Tokens.tokIdentifier:
                    {
                        Ident = sym.Text;

                        GetNextSymbol();

                        switch (sym.Token)
                        {
                            case Symbol.Tokens.tokEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokPlusEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokMinusEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokMultiplicationEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokDivisionEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokStringConcatEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokDivEq:
                                StatementComparativeOperators(Ident);
                                break;

                            case Symbol.Tokens.tokModEq:
                                StatementComparativeOperators(Ident);
                                break;

                            default:
                                {
                                    CallUserdefinedFunction(Ident);
                                    break;
                                }
                        }

                        break;
                    }

                default:
                    {
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: declaration, function call or assignment", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }
            }
        }

        private void StatementComparativeOperators(string Ident)
        {
            var op = sym.Token;

            if (symboltable!.Exists(Ident, null, Identifier.IdentifierTypes.IdConst))
            {
                errorObject!.Raise((int)(int)InterpreterError.ParsErrors.errIdentifierAlreadyExists,
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

        private void StatementList(bool singleLineOnly, bool allowFunctionDeclarations, int exitsAllowed, params Symbol.Tokens[] endSymbols)
        {
            do
            {
                // Anweisungstrenner (":" und Zeilenwechsel) überspringen
                while (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                {
                    if (sym.Text == "\n" & singleLineOnly) { return; }

                    GetNextSymbol();
                }

                // Wenn eines der übergebenen Endesymbole erreicht ist, dann
                // Listenverarbeitung beenden
                if (endSymbols.Contains(sym.Token)) { return; }

                // Alles ok, die nächste Anweisung liegt an...
                Statement(singleLineOnly, allowFunctionDeclarations, exitsAllowed);
            }
            while (code!.ErrorObject!.Number == 0);
        }

        private void Term()
        {
            // Factor { ("*" | "/" | "\" | "DIV") Factor }
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
                        {
                            code!.Add(Opcodes.Multiplication);
                            break;
                        }

                    case Symbol.Tokens.tokDivision:
                        {
                            code!.Add(Opcodes.Division);
                            break;
                        }

                    case Symbol.Tokens.tokDiv:
                        {
                            code!.Add(Opcodes.Div);
                            break;
                        }
                }
            }
        }

        private void Terminal()
        {
            Symbol.Tokens operator_Renamed;
            int thenPC, elsePC;
            string ident;
            switch (sym.Token)
            {
                case Symbol.Tokens.tokMinus // "-" Terminal
               :
                    {
                        GetNextSymbol();

                        Terminal();

                        code!.Add(Opcodes.Negate);
                        break;
                    }

                case Symbol.Tokens.tokNot // "NOT" Terminal
         :
                    {
                        GetNextSymbol();

                        Terminal();

                        code!.Add(Opcodes.Not);
                        break;
                    }

                case Symbol.Tokens.tokNumber:
                    {
                        code!.Add(Opcodes.PushValue, sym.Value!);

                        GetNextSymbol();
                        break;
                    }
                case Symbol.Tokens.tokString:
                    {
                        code!.Add(Opcodes.PushValue, sym.Value!);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokIdentifier // Identifier [ "(" Condition { "," Condition } ]
         :
                    {
                        // folgt hinter dem Identifier eine "(" so sind danach Funktionsparameter zu erwarten

                        // Wurde Identifier überhaupt schon deklariert?
                        if (optionExplicit & !symboltable!.Exists(sym.Text))
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.Terminal", "Identifier '" + sym.Text + "' has not be declared", sym.Line, sym.Col, sym.Index, sym.Text);

                        if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.IdFunction))
                        {
                            // Userdefinierte Funktion aufrufen
                            ident = sym.Text;

                            GetNextSymbol();

                            CallUserdefinedFunction(ident);

                            code!.Add(Opcodes.PushVariable, ident); // Funktionsresultat auf den Stack
                        }
                        else if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.IdSub))
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errCannotCallSubInExpression, "SyntaxAnalyser.Terminal", "Cannot call sub '" + sym.Text + "' in expression", sym.Line, sym.Col, sym.Index);
                        else
                        {
                            // Wert einer Variablen bzw. Konstante auf den Stack legen

                            code!.Add(Opcodes.PushVariable, sym.Text);
                            GetNextSymbol();
                        }

                        break;
                    }

                case Symbol.Tokens.tokTrue:
                    {
                        code!.Add(Opcodes.PushValue, true);
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokFalse:
                    {
                        code!.Add(Opcodes.PushValue, false);
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokPi:
                    {
                        code!.Add(Opcodes.PushValue, 3.141592654);
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokCrlf:
                    {
                        code!.Add(Opcodes.PushValue, "\r\n");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokTab:
                    {
                        code!.Add(Opcodes.PushValue, "\t");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokCr:
                    {
                        code!.Add(Opcodes.PushValue, "\r");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokLf:
                    {
                        code!.Add(Opcodes.PushValue, "\n");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokMsgbox:
                    operator_Renamed = Boxes(sym.Token);
                    break;

                case Symbol.Tokens.tokInputbox:
                    CallInputbox(false);
                    GetNextSymbol();
                    //operator_Renamed = Boxes(sym.Token);
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

                case Symbol.Tokens.tokIif // "IIF" "(" Condition "," Condition "," Condition ")"
         :
                    {
                        GetNextSymbol();
                        if (sym.Token == Symbol.Tokens.tokLeftParent)
                        {
                            GetNextSymbol();

                            Condition();

                            thenPC = code!.Add(Opcodes.JumpFalse);

                            if (sym.Token == Symbol.Tokens.tokComma)
                            {
                                GetNextSymbol();

                                Condition(); // true Value

                                elsePC = code.Add(Opcodes.Jump);
                                code.FixUp(thenPC - 1, code.EndOfCodePc);

                                if (sym.Token == Symbol.Tokens.tokComma)
                                {
                                    GetNextSymbol();

                                    Condition(); // false Value

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
                    }

                case Symbol.Tokens.tokLeftParent // "(" Condition ")"
         :
                    {
                        GetNextSymbol();

                        Condition();

                        if (sym.Token == Symbol.Tokens.tokRightParent)
                            GetNextSymbol();
                        else
                            errorObject!.Raise((int)InterpreterError.ParsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')'", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokEof:
                    {
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Identifier or function or '(' expected but end of source found", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                default:
                    {
                        errorObject!.Raise((int)InterpreterError.ParsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Expected: expression; found symbol '" + sym.Text + "'", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }
            }
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
    }
}
