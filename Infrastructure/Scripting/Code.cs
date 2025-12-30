using System.Globalization;

namespace Klacks.Api.Infrastructure.Scripting
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

        private const int MaxRecursionDepth = 1000;

        private List<object> code = new();

        private Scope external = new();

        private int pc;

        private int recursionDepth;

        private bool running;

        private Scopes? scopes;

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

            code = new List<object>();

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

        public void ImportClear()
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

        public bool Run(CancellationToken cancellationToken = default)
        {
            if (ErrorObject?.Number == 0)
            {
                Interpret(cancellationToken);
                return ErrorObject.Number == 0;
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

        internal object GetInstruction(int index)
        {
            return code[index];
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

            if (register == null || accumulator == null)
            {
                return;
            }

            try
            {
                var opcode = (Opcodes)operation.GetValue(0)!;

                switch (opcode)
                {
                    case Opcodes.Add:
                        scopes.Push(Helper.ExtractDouble(accumulator) + Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Sub:
                        scopes.Push(Helper.ExtractDouble(accumulator) - Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Multiplication:
                        scopes.Push(Helper.ExtractDouble(accumulator) * Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Division:
                        scopes.Push(Helper.ExtractDouble(accumulator) / Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Div:
                        scopes.Push((int)Helper.ExtractDouble(accumulator) / (int)Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Mod:
                        scopes.Push(Helper.ExtractDouble(accumulator) % Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Power:
                        scopes.Push(Math.Pow(Helper.ExtractDouble(accumulator), Helper.ExtractDouble(register)));
                        break;
                    case Opcodes.StringConcat:
                        scopes.Push(Helper.ExtractString(accumulator) + Helper.ExtractString(register));
                        break;
                    case Opcodes.Or:
                        scopes.Push(Helper.ExtractInt(accumulator) | Helper.ExtractInt(register));
                        break;
                    case Opcodes.And:
                        scopes.Push(Helper.ExtractInt(accumulator) & Helper.ExtractInt(register));
                        break;
                    case Opcodes.Eq:
                        scopes.Push(Helper.ExtractString(accumulator).Equals(Helper.ExtractString(register)));
                        break;
                    case Opcodes.NotEq:
                        scopes.Push(!Helper.ExtractString(accumulator).Equals(Helper.ExtractString(register)));
                        break;
                    case Opcodes.Lt:
                        scopes.Push(Helper.ExtractDouble(accumulator) < Helper.ExtractDouble(register));
                        break;
                    case Opcodes.LEq:
                        scopes.Push(Helper.ExtractDouble(accumulator) <= Helper.ExtractDouble(register));
                        break;
                    case Opcodes.Gt:
                        scopes.Push(Helper.ExtractDouble(accumulator) > Helper.ExtractDouble(register));
                        break;
                    case Opcodes.GEq:
                        scopes.Push(Helper.ExtractDouble(accumulator) >= Helper.ExtractDouble(register));
                        break;
                }
            }
            catch (Exception ex)
            {
                running = false;
                ErrorObject!.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run", "Error during calculation (binary op " + operation!.GetValue(0)!.ToString() + "): " + ex.HResult + "(" + ex.Message + ")", 0, 0, 0);
            }
        }

        private int Factorial(int n)
        {
            if (n == 0)
                return 1;
            else
                return n * Factorial(n - 1);
        }

        private void Interpret(CancellationToken cancellationToken)
        {
            object?[] operation;
            object? akkumulator;
            object? register;

            scopes = new Scopes();
            scopes.PushScope(external);
            scopes.PushScope();

            Cancel = false;
            running = true;
            recursionDepth = 0;

            pc = 0;

            bool accepted;
            object xPos;
            object defaultRenamed;
            object yPos;

            while ((pc < code.Count) & running)
            {
                akkumulator = null;
                register = null;

                operation = (object[])code[pc];
                switch ((Opcodes)operation.GetValue(0)!)
                {
                    case Opcodes.AllocConst:
                        {
                            scopes.Allocate(operation.GetValue(1)!.ToString()!, operation.GetValue(2)!.ToString(), Identifier.IdentifierTypes.IdConst);
                            break;
                        }

                    case Opcodes.AllocVar:
                        {
                            scopes.Allocate(operation.GetValue(1)!.ToString()!);
                            break;
                        }

                    case Opcodes.PushValue:
                        {
                            scopes.Push(operation.GetValue(1)!);
                            break;
                        }

                    case Opcodes.PushVariable:
                        {
                            string? name;
                            try
                            {
                                var tmp = operation.GetValue(1)!;
                                name = tmp is Identifier id ? id.Value?.ToString() : tmp.ToString();
                                register = scopes.Retrieve(name!);
                            }
                            catch (Exception)
                            {
                                accepted = false;
                                Retrieve?.Invoke(operation.GetValue(1)!.ToString()!, register!.ToString()!, accepted);
                                if (!accepted)
                                {
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
                                scopes.Push(register is Identifier regId ? regId.Value! : register!.ToString()!);
                            }

                            break;
                        }

                    case Opcodes.Pop:
                        {
                            scopes.PopScopes();
                            break;
                        }

                    case Opcodes.PopWithIndex:
                        {
                            register = scopes.Pop(Convert.ToInt32(operation.GetValue(1)));
                            var result = register is Identifier popId ? popId.Value! : register!;
                            scopes.Push(result);
                            break;
                        }

                    case Opcodes.Assign:
                        {
                            register = scopes.Pop();
                            var result = register is Identifier assignId ? assignId.Value! : register!;

                            if (!scopes.Assign(operation.GetValue(1)!.ToString()!, result))
                            {
                                accepted = false;

                                Assign?.Invoke(operation.GetValue(1)!.ToString()!, result!.ToString()!, ref accepted);
                                if (!accepted)
                                {
                                    scopes.Allocate(operation.GetValue(1)!.ToString()!, result.ToString());
                                }
                            }

                            break;
                        }

                    case Opcodes.Add:
                    case Opcodes.Sub:
                    case Opcodes.Multiplication:
                    case Opcodes.Division:
                    case Opcodes.Div:
                    case Opcodes.Mod:
                    case Opcodes.Power:
                    case Opcodes.StringConcat:
                    case Opcodes.Or:
                    case Opcodes.And:
                    case Opcodes.Eq:
                    case Opcodes.NotEq:
                    case Opcodes.Lt:
                    case Opcodes.LEq:
                    case Opcodes.Gt:
                    case Opcodes.GEq:
                        BinaryMathOperators(operation!, akkumulator!, register!);
                        break;

                    case Opcodes.Negate:
                    case Opcodes.Not:
                    case Opcodes.Factorial:
                    case Opcodes.Sin:
                    case Opcodes.Cos:
                    case Opcodes.Tan:
                    case Opcodes.ATan:
                        UnaryMathOperators(operation!);
                        break;

                    case Opcodes.DebugPrint:
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

                    case Opcodes.DebugClear:
                        {
                            DebugClear?.Invoke();
                            break;
                        }

                    case Opcodes.DebugShow:
                        {
                            DebugShow?.Invoke();
                            break;
                        }

                    case Opcodes.DebugHide:
                        {
                            DebugHide?.Invoke();
                            break;
                        }

                    case Opcodes.Message:
                        {
                            try
                            {
                                register = scopes.PopScopes();
                                akkumulator = scopes.PopScopes().Value;

                                var msg = register is Identifier msgId ? msgId.Value?.ToString() ?? string.Empty : register?.ToString() ?? string.Empty;
                                var type = akkumulator is Identifier typeId ? Convert.ToInt32(typeId.Value) : Convert.ToInt32(akkumulator ?? 0);

                                Message?.Invoke(type, msg);
                            }
                            catch (Exception)
                            {
                                Message?.Invoke(-1, string.Empty);
                            }

                            break;
                        }

                    case Opcodes.Msgbox:
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

                    case Opcodes.DoEvents:
                        {
                            break;
                        }

                    case Opcodes.Inputbox:
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

                    case Opcodes.Jump:
                        {
                            pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.JumpTrue:
                    case Opcodes.JumpFalse:
                        {
                            akkumulator = scopes.PopScopes().Value;
                            if (!Convert.ToBoolean(akkumulator))
                            {
                                pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            }

                            break;
                        }

                    case Opcodes.JumpPop:
                        {
                            pc = Convert.ToInt32(scopes.PopScopes().Value) - 1;
                            break;
                        }

                    case Opcodes.PushScope:
                        {
                            scopes.PushScope();
                            break;
                        }

                    case Opcodes.PopScope:
                        {
                            scopes.PopScopes();
                            break;
                        }

                    case Opcodes.Call:
                        {
                            if (++recursionDepth > MaxRecursionDepth)
                            {
                                running = false;
                                throw new ScriptTooComplexException($"Maximum recursion depth ({MaxRecursionDepth}) exceeded");
                            }
                            scopes.Allocate("~RETURNADDR", (pc + 1).ToString(), Identifier.IdentifierTypes.IdConst);
                            pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.Return:
                        {
                            recursionDepth--;
                            pc = Convert.ToInt32(Convert.ToDouble(scopes.Retrieve("~RETURNADDR")!.Value, CultureInfo.InvariantCulture) - 1);
                            break;
                        }
                }

                pc++;

                if (Cancel || cancellationToken.IsCancellationRequested)
                {
                    running = false;
                    ErrorObject!.Raise((int)InterpreterError.RunErrors.errCancelled, "Code.Run", "Code execution aborted", 0, 0, 0);
                }
            }

            running = false;
        }

        private void UnaryMathOperators(object[] operation)
        {
            var akkumulator = scopes!.PopScopes().Value;

            if (akkumulator == null)
            {
                return;
            }

            try
            {
                var opcode = (Opcodes)operation.GetValue(0)!;
                double number = Helper.ExtractDouble(akkumulator);

                switch (opcode)
                {
                    case Opcodes.Negate:
                        scopes.Push(-number);
                        break;
                    case Opcodes.Not:
                        scopes.Push(!Convert.ToBoolean(akkumulator));
                        break;
                    case Opcodes.Factorial:
                        scopes.Push(Factorial((int)number));
                        break;
                    case Opcodes.Sin:
                        scopes.Push(Math.Sin(number));
                        break;
                    case Opcodes.Cos:
                        scopes.Push(Math.Cos(number));
                        break;
                    case Opcodes.Tan:
                        scopes.Push(Math.Tan(number));
                        break;
                    case Opcodes.ATan:
                        scopes.Push(Math.Atan(number));
                        break;
                }
            }
            catch (Exception ex)
            {
                running = false;
                ErrorObject?.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run", "Error during calculation (unary op " + operation.GetValue(0)?.ToString() + "): " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
            }
        }
    }
}
