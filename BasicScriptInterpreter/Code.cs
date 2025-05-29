using Klacks.Api.BasicScriptInterpreter.Macro.Process;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Klacks.Api.BasicScriptInterpreter
{
    public class Code : IDisposable
    {
        #region Events

        public delegate void AssignEventHandler(string name, string value, ref bool accepted);

        public delegate void DebugClearEventHandler();

        public delegate void DebugHideEventHandler();

        public delegate void DebugPrintEventHandler(string msg);

        public delegate void DebugShowEventHandler();

        public delegate void MessageEventHandler(int type, string message);

        public delegate void RetrieveEventHandler(string name, string value, bool accepted);

        public event AssignEventHandler? Assign;

        public event DebugClearEventHandler? DebugClear;

        public event DebugHideEventHandler? DebugHide;

        public event DebugPrintEventHandler? DebugPrint;

        public event DebugShowEventHandler? DebugShow;

        public event MessageEventHandler? Message;

        public event RetrieveEventHandler? Retrieve;

        #endregion Events

        private Collection<object> code = new();

        private Scope external = new();

        private int pc; // Program Counter = zeigt auf aktuelle Anweisung in code

        private bool running;

        private Scopes? scopes;

        ~Code()
        {
            Dispose();
        }

        /// <summary>
        /// Befehlscodes der Virtuellen Maschine (VM)
        /// Die VM ist im wesentlichen eine Stack-Maschine. Die meisten
        /// für die Operationen relevanten Parameter befinden sich
        /// jeweils auf dem Stack, wenn der Befehl zur Ausführung kommt.
        /// </summary>
        public enum Opcodes
        {
            OpAllocConst = 0,
            OpAllocVar = 1,
            opPushValue = 2,

            opPushVariable,
            opPop,
            opPopWithIndex,

            // 6
            opAssign,

            // 7
            opAdd,

            opSub,
            opMultiplication,
            opDivision,
            opDiv,
            opMod,
            opPower,
            opStringConcat,
            opOr,
            opAnd,
            opEq,
            opNotEq,
            oplt,
            opLEq,
            opGt,
            opGEq,

            // 23
            opNegate,

            opNot,
            opFactorial,
            opSin,
            opCos,
            opTan,
            opATan,

            // 30
            opDebugPrint,

            opDebugClear,
            opDebugShow,
            opDebugHide,
            opMsgbox,
            opDoEvents,
            opInputbox,

            // 37
            opJump,

            opJumpTrue,
            opJumpFalse,
            opJumpPop,

            // 41
            opPushScope,

            opPopScope,
            opCall,
            opReturn,
            opMessage
        }

        public interface IInputStream
        {
            int Col { get; }   // aktuelle Spalte im Code

            bool Eof { get; } // Ist das Code-Ende erreicht?

            InterpreterError ErrorObject { get; set; } //  Wenn diese Funktion für eine bestimmte Quelle nicht realisierbar ist, muss ein Fehler erzeugt werden: errGoBackPastStartOfSource oder errGoBackNotImplemented

            int Index { get; } // aktuelle Index im Code

            int Line { get; } // aktuelle Zeilennummer im Code

            IInputStream Connect(string connectString); // Objekt an Script-Code anbinden

            string GetNextChar(); // Nächstes Zeichen aus Code herauslesen

            void GoBack(); // Um 1 Zeichen im Code zurückgehen

            void SkipComment(); // Kommentar im Code überspringe
        }

        public bool AllowUi { get; set; }

        public bool Cancel { get; set; }

        public int CodeTimeout { get; set; } = 120000;

        public int Count
        {
            get
            {
                return code.Count;
            }
        }

        /// <summary>
        /// Gets fehlerobjekt lesenetzen oder Fehlerobjekt s.
        /// </summary>
        public InterpreterError? ErrorObject { get; private set; }

        public bool Running { get; private set; }

        internal int EndOfCodePc
        {
            get
            {
                return code.Count;
            }
        }

        public Code Clone()
        {
            var clone = new Code();
            foreach (var item in code)
            {
                clone.CloneAdd(item);
            }

            for (int i = 0; i <= external.CloneCount() - 1; i++)
            {
                clone.ImportAdd(((Identifier)external.CloneItem(i)).Name!);
            }

            clone.ErrorObject = new InterpreterError();

            return clone;
        }

        public bool Compile(string source, bool optionExplicit = true, bool allowExternal = true)
        {
            ErrorObject = new InterpreterError();

            StringInputStream sourceStream = new();

            var parser = new SyntaxAnalyser(ErrorObject);

            code = new Collection<object>();

            parser.Parse(sourceStream.Connect(source), this, optionExplicit, allowExternal);

            if (ErrorObject.Number == 0)
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            ErrorObject = null;
        }

        public Identifier ImportAdd(string name, object? value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.IdVariable)
        {
            return external.Allocate(name, value, idType);
        }

        public void ImportClear() // Globalen Gültigkeitsbereich löschen
        {
            external = new Scope();
        }

        public void ImportItem(string name, object? value = null)
        {
            external.Assign(name, value!);
        }

        public object ImportRead(string name)
        {
            return external.Retrieve(name);
        }

        public bool Run()
        {
            if (ErrorObject?.Number == 0)
            {
                Interpret();
                return Convert.ToBoolean(ErrorObject.Number == 0);
            }

            return false;
        }

        internal int Add(Opcodes opCode, params object[] parameters)
        {
            object[] operation = new object[parameters.Length + 1];
            operation[0] = opCode;

            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                operation[i + 1] = parameters[i];
            }

            code.Add(operation);

            return code.Count;
        }

        internal void CloneAdd(object value)
        {
            code.Add(value);
        }

        internal Scope External()
        {
            return external;
        }

        internal void FixUp(int index, params object[] parameters)
        {
            object[] operation = new object[parameters.Length + 1];
            var tmp = (object[])code[index];
            operation[0] = tmp[0];

            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                operation[i + 1] = parameters[i];
            }

            code.RemoveAt(index);

            if (index > code.Count)
            {
                code.Add(operation);
            }
            else
            {
                code.Insert(index, operation);
            }
        }

        private void BinaryMathOperators(object[] operation, object accumulator, object register)
        {
            register = scopes!.PopScopes();
            accumulator = scopes.PopScopes();

            if (register != null && accumulator != null)
            {
                try
                {
                    switch ((Opcodes)operation.GetValue(0)!)
                    {
                        case Opcodes.opAdd:
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(tmpAk + tmpReg);
                                break;
                            }

                        case Opcodes.opSub:
                            {
                                var tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(tmpAk - tmpReg);
                                break;
                            }

                        case Opcodes.opMultiplication:
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture) * Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opDivision:
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture) / Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opDiv:
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(Convert.ToInt32(tmpAk, CultureInfo.InvariantCulture) / Convert.ToInt32(tmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opMod:
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture) % Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opPower:
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                {
                                    tmpAk = Convert.ToDouble(((Identifier)accumulator).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(accumulator))
                                {
                                    tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                                }

                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                {
                                    tmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                }
                                else if (Helper.IsNumericDouble(register))
                                {
                                    tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                }

                                scopes.Push(Math.Pow(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture), Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture)));
                                break;
                            }

                        case Opcodes.opStringConcat:
                            {
                                string tmpAk;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToString(((Identifier)accumulator).Value)!;
                                else
                                    tmpAk = Convert.ToString(accumulator)!;
                                string tmpReg;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToString(((Identifier)register).Value)!;
                                else
                                    tmpReg = Convert.ToString(register)!;
                                scopes.Push(tmpAk + tmpReg);
                                break;
                            }

                        case Opcodes.opOr:
                            {
                                int tmpAk = 0;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToInt32(((Identifier)accumulator).Value);
                                else if (Helper.IsNumericInt(accumulator))
                                    tmpAk = Convert.ToInt32(accumulator);
                                int tmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToInt32(register);
                                scopes.Push(tmpAk | tmpReg);
                                break;
                            }

                        case Opcodes.opAnd:
                            {
                                int tmpAk = 0;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToInt32(((Identifier)accumulator).Value);
                                else if (Helper.IsNumericInt(accumulator))
                                    tmpAk = Convert.ToInt32(accumulator);
                                int tmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToInt32(register);
                                scopes.Push(tmpAk & tmpReg);
                                break;
                            }

                        case Opcodes.opEq // =
                 :
                            {
                                string tmpAk;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToString(((Identifier)accumulator!).Value)!;
                                else
                                    tmpAk = Convert.ToString(accumulator)!;
                                string tmpReg;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToString(((Identifier)register).Value)!;
                                else
                                    tmpReg = Convert.ToString(register)!;
                                scopes.Push(tmpAk!.Equals(tmpReg));
                                break;
                            }

                        case Opcodes.opNotEq // <>
                 :
                            {
                                string tmpAk;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToString(((Identifier)accumulator).Value)!;
                                else
                                    tmpAk = Convert.ToString(accumulator)!;
                                string tmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToString(((Identifier)register).Value)!;
                                else if (!Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToString(register)!;
                                scopes.Push(!tmpAk!.Equals(tmpReg));
                                break;
                            }

                        case Opcodes.oplt // <
                 :
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToInt32(((Identifier)accumulator).Value);
                                else if (Helper.IsNumericInt(accumulator))
                                    tmpAk = Convert.ToInt32(accumulator);
                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToInt32(register);
                                scopes.Push(tmpAk < tmpReg);
                                break;
                            }

                        case Opcodes.opLEq // <=
                 :
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToInt32(((Identifier)accumulator).Value);
                                else if (Helper.IsNumericInt(accumulator))
                                    tmpAk = Convert.ToInt32(accumulator);
                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToInt32(register);
                                scopes.Push(tmpAk <= tmpReg);
                                break;
                            }

                        case Opcodes.opGt // >
                 :
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToInt32(((Identifier)accumulator).Value);
                                else if (Helper.IsNumericInt(accumulator))
                                    tmpAk = Convert.ToInt32(accumulator);
                                double tmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToInt32(register);
                                scopes.Push(tmpAk > tmpReg);
                                break;
                            }

                        case Opcodes.opGEq // >=
                 :
                            {
                                double tmpAk = 0.0D;
                                if (accumulator.GetType() == typeof(Identifier))
                                    tmpAk = Convert.ToInt32(((Identifier)accumulator).Value);
                                else if (Helper.IsNumericInt(accumulator))
                                    tmpAk = Convert.ToInt32(accumulator);
                                double tmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    tmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    tmpReg = Convert.ToInt32(register);
                                scopes.Push(tmpAk >= tmpReg);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    running = false;
                    ErrorObject!.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run", "Error during calculation (binary op " + operation!.GetValue(0)!.ToString() + "): " + ex.HResult + "(" + ex.Message + ")", 0, 0, 0);
                }
            }
        }

        // Hilfsfunktion zur Fakultätsberechnung
        private int Factorial(int n)
        {
            if (n == 0)
                return 1;
            else
                return n * Factorial(n - 1);
        }

        private void Interpret()
        {
            object?[] operation;
            object? akkumulator;
            object? register;

            scopes = new Scopes();
            scopes.PushScope(external);
            scopes.PushScope();

            int startTime = Environment.TickCount;
            Cancel = false;
            running = true;

            pc = 0;

            bool accepted;
            object xPos;
            object defaultRenamed;
            object yPos;

            while ((pc < code.Count) & running)
            {
                akkumulator = null;
                register = null;

#pragma warning disable SA1121 // UseBuiltInTypeAlias
                operation = (Object[])code[pc];
#pragma warning restore SA1121 // UseBuiltInTypeAlias
                switch ((Opcodes)operation.GetValue(0)!)
                {
                    case Opcodes.OpAllocConst: // Konstante allozieren
                        {
                            // Parameter:    Name der Konstanten; Wert
                            scopes.Allocate(operation.GetValue(1)!.ToString()!, operation.GetValue(2)!.ToString(), Identifier.IdentifierTypes.IdConst);
                            break;
                        }

                    case Opcodes.OpAllocVar: // Variable allozieren
                        {
                            // Parameter:    Name der Variablen
                            scopes.Allocate(operation.GetValue(1)!.ToString()!);
                            break;
                        }

                    case Opcodes.opPushValue: // Wert auf den Stack schieben
                        {
                            // Parameter:    Wert
                            scopes.Push(operation.GetValue(1)!);
                            break;
                        }

                    case Opcodes.opPushVariable: // Wert einer Variablen auf den Stack schieben
                        {
                            // Parameter:    Variablenname
                            string? name;
                            try
                            {
                                var tmp = operation.GetValue(1)!;
                                if (tmp.GetType() == typeof(Identifier))
                                {
                                    name = ((Identifier)tmp).Value!.ToString();
                                }
                                else
                                {
                                    name = tmp.ToString();
                                }

                                register = scopes.Retrieve(name!);
                            }
                            catch (Exception)
                            {
                                // Variable nicht alloziert, also bei Host nachfragen
                                accepted = false;
                                Retrieve?.Invoke(operation.GetValue(1)!.ToString()!, register!.ToString()!, accepted);
                                if (!accepted)
                                {
                                    // der Host weiss nichts von der Var. Implizit anlegen tun wir
                                    // sie aber nicht, da sie hier auf sie sofort lesend zugegriffen
                                    // würde
                                    running = false;
                                    ErrorObject!.Raise((int)InterpreterError.RunErrors.errUnknownVar, "Code.Run", "Unknown variable '" + operation.GetValue(1) + "'", 0, 0, 0);
                                }
                            }

                            if (register == null)
                            {
                                running = false;
                                ErrorObject!.Raise((int)InterpreterError.RunErrors.errUninitializedVar, "Code.Run", "Variable '" + operation.GetValue(1) + "' not hasn´t been assigned a Value yet", 0, 0, 0);
                            }
                            else
                            {
                                if (register.GetType() == typeof(Identifier))
                                {
                                    scopes.Push(((Identifier)register!).Value!);
                                }
                                else
                                {
                                    scopes.Push(register!.ToString()!);
                                }
                            }

                            break;
                        }

                    case Opcodes.opPop: // entfernt obersten Wert vom Stack
                        {
                            scopes.PopScopes();
                            break;
                        }

                    case Opcodes.opPopWithIndex: // legt den n-ten Stackwert zuoberst auf den Stack
                        {
                            // Parameter:    Index in den Stack (von oben an gezählt: 0..n)
                            object result;
                            register = scopes.Pop(Convert.ToInt32(operation.GetValue(1)));
                            if (register is Identifier)
                            {
                                result = ((Identifier)register).Value!;
                            }
                            else
                            {
                                result = register!;
                            }

                            scopes.Push(result!);
                            break;
                        }

                    case Opcodes.opAssign: // Wert auf dem Stack einer Variablen zuweisen
                        {
                            // Parameter:    Variablenname
                            // Stack:        der zuzuweisende Wert
                            object result;
                            register = scopes.Pop();
                            if (register is Identifier)
                            {
                                result = ((Identifier)register).Value!;
                            }
                            else
                            {
                                result = register!;
                            }

                            if (!scopes.Assign(operation.GetValue(1)!.ToString()!, result))
                            {
                                // Variable nicht alloziert, also Host anbieten
                                accepted = false;

                                Assign?.Invoke(operation.GetValue(1)!.ToString()!, result!.ToString()!, ref accepted);
                                if (!accepted)
                                {
                                    // Host hat nicht mit Var am Hut, dann legen wir
                                    // sie eben selbst an
                                    scopes.Allocate(operation.GetValue(1)!.ToString()!, result.ToString());
                                }
                            }

                            break;
                        }

                    case Opcodes.opAdd:
                    case Opcodes.opSub:
                    case Opcodes.opMultiplication:
                    case Opcodes.opDivision:
                    case Opcodes.opDiv:
                    case Opcodes.opMod:
                    case Opcodes.opPower:
                    case Opcodes.opStringConcat:
                    case Opcodes.opOr:
                    case Opcodes.opAnd:
                    case Opcodes.opEq:
                    case Opcodes.opNotEq:
                    case Opcodes.oplt:
                    case Opcodes.opLEq:
                    case Opcodes.opGt:
                    case Opcodes.opGEq:
                        BinaryMathOperators(operation!, akkumulator!, register!);
                        break;

                    case Opcodes.opNegate:
                    case Opcodes.opNot:
                    case Opcodes.opFactorial:
                    case Opcodes.opSin:
                    case Opcodes.opCos:
                    case Opcodes.opTan:
                    case Opcodes.opATan:
                        UnaryMathOperators(operation!);
                        break;

                    case Opcodes.opDebugPrint:
                        {
                            string msg = string.Empty;

                            register = scopes.PopScopes();
                            if (register != null)
                            {
                                msg = ((Identifier)register)!.Value!.ToString()!;
                            }

                            DebugPrint?.Invoke(msg!);

                            break;
                        }

                    case Opcodes.opDebugClear:
                        {
                            DebugClear?.Invoke();
                            break;
                        }

                    case Opcodes.opDebugShow:
                        {
                            DebugShow?.Invoke();
                            break;
                        }

                    case Opcodes.opDebugHide:
                        {
                            DebugHide?.Invoke();
                            break;
                        }

                    case Opcodes.opMessage:
                        {
                            try
                            {
                                string msg = string.Empty;
                                int type = 0;
                                register = scopes.PopScopes(); // Message
                                akkumulator = scopes.PopScopes().Value; // Type
                                if (register is Identifier)
                                {
                                    if (register != null)
                                    {
                                        if (register.GetType() == typeof(Identifier)) { msg = ((Identifier)register).Value!.ToString()!; }
                                        else
                                        {
                                            msg = register.ToString()!;
                                        }
                                    }
                                }

                                if (akkumulator != null)
                                {
                                    if (akkumulator.GetType() == typeof(Identifier)) { type = Convert.ToInt32(((Identifier)akkumulator).Value); }
                                    else
                                    {
                                        type = Convert.ToInt32(akkumulator);
                                    }
                                }

                                Message?.Invoke(type, msg);
                            }
                            catch (Exception)
                            {
                                Message?.Invoke(-1, string.Empty);
                            }

                            break;
                        }

                    case Opcodes.opMsgbox:
                        {
                            if (!AllowUi)
                            {
                                running = false;
                                ErrorObject!.Raise((int)InterpreterError.RunErrors.errNoUIallowed, "Code.Run", "MsgBox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                            }

                            register = scopes.PopScopes().Value; // Title
                            akkumulator = scopes.PopScopes().Value; // Buttons

                            try
                            {
                                // TODO:InputBox  // scopes.Push(MsgBox(scopes.Pop, (MsgBoxStyle)Akkumulator.ToString(), RegisterUser));
                            }
                            catch (Exception ex)
                            {
                                running = false;
                                ErrorObject!.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run", "Error during MsgBox-call: " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
                            }

                            break;
                        }

                    case Opcodes.opDoEvents:
                        {
                            break;
                        }

                    case Opcodes.opInputbox:
                        {
                            if (!AllowUi)
                            {
                                running = false;
                                ErrorObject!.Raise((int)InterpreterError.RunErrors.errNoUIallowed, "Code.Run", "Inputbox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                            }

                            yPos = scopes.PopScopes().Value!;
                            xPos = scopes.PopScopes().Value!;
                            defaultRenamed = scopes.PopScopes().Value!;
                            register = scopes.PopScopes().Value;
                            akkumulator = scopes.PopScopes().Value;

                            try
                            {
                                // TODO:InputBox
                                // string Anwert = Microsoft.VisualBasic.Interaction.InputBox(Akkumulator.ToString(), RegisterUser.ToString(), defaultRenamed.ToString(), Convert.ToInt32(xPos), Convert.ToInt32(yPos));
                                // scopes.Push(Anwert);
                            }
                            catch (Exception ex)
                            {
                                running = false;
                                ErrorObject!.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run", "Error during MsgBox-call: " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
                            }

                            break;
                        }

                    case Opcodes.opJump:
                        {
                            pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpTrue:
                    case Opcodes.opJumpFalse:
                        {
                            akkumulator = scopes.PopScopes().Value;
                            if (!Convert.ToBoolean(akkumulator))
                            {
                                pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            }

                            break;
                        }

                    case Opcodes.opJumpPop:
                        {
                            pc = Convert.ToInt32(scopes.PopScopes().Value) - 1;
                            break;
                        }

                    case Opcodes.opPushScope:
                        {
                            scopes.PushScope();
                            break;
                        }

                    case Opcodes.opPopScope:
                        {
                            scopes.PopScopes();
                            break;
                        }

                    case Opcodes.opCall:
                        {
                            scopes.Allocate("~RETURNADDR", (pc + 1).ToString(), Identifier.IdentifierTypes.IdConst);
                            pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opReturn:
                        {
                            pc = Convert.ToInt32(Convert.ToDouble(scopes.Retrieve("~RETURNADDR")!.Value, CultureInfo.InvariantCulture) - 1);
                            break;
                        }
                }

                pc++; // zum nächsten Befehl

                if (Cancel) // wurde Interpretation unterbrochen?
                {
                    running = false;
                    ErrorObject!.Raise((int)InterpreterError.RunErrors.errCancelled, "Code.Run", "Code execution aborted", 0, 0, 0);
                }

                var tickPassed = Environment.TickCount - startTime;
                if (CodeTimeout > 0 && tickPassed >= CodeTimeout) // Timeout erreicht?
                {
                    running = false;
                    ErrorObject!.Raise((int)InterpreterError.RunErrors.errTimedOut, "Code.Run", "Timeout reached: code execution has been aborted", 0, 0, 0);
                }
            }

            running = false;
        }

        private void UnaryMathOperators(object[] operation)
        {
            var akkumulator = scopes!.PopScopes().Value;

            try
            {
                switch ((Opcodes)operation.GetValue(0)!)
                {
                    case Opcodes.opNegate:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator!.ToString()!);
                            scopes.Push(number * -1);
                            break;
                        }

                    case Opcodes.opNot:
                        {
                            var tmp = Convert.ToBoolean(akkumulator);
                            scopes.Push(!tmp);
                            break;
                        }

                    case Opcodes.opFactorial:
                        {
                            scopes.Push(Factorial(Convert.ToInt32(akkumulator)));
                            break;
                        }

                    case Opcodes.opSin:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator!.ToString()!);
                            scopes.Push(Math.Sin(number));
                            break;
                        }

                    case Opcodes.opCos:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator!.ToString()!);
                            scopes.Push(Math.Cos(number));
                            break;
                        }

                    case Opcodes.opTan:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator!.ToString()!);
                            scopes.Push(Math.Tan(number));
                            break;
                        }

                    case Opcodes.opATan:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator!.ToString()!);
                            scopes.Push(Math.Atan(number));
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                running = false;
#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
                ErrorObject.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run", "Error during calculation (unary op " + operation.GetValue(0).ToString() + "): " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
#pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
            }
        }
    }
}
