using System.Globalization;

namespace Klacks.Api.Infrastructure.Scripting;

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
    public int Count => code.Count;
    public InterpreterError? ErrorObject { get; private set; }
    public bool Running { get; private set; }
    internal int EndOfCodePc => code.Count;

    public Code Clone()
    {
        var clone = new Code();
        foreach (var item in code)
        {
            clone.CloneAdd(item);
        }

        for (int i = 0; i < external.CloneCount(); i++)
        {
            clone.ImportAdd(external.CloneItem(i).Name!);
        }

        clone.ErrorObject = new InterpreterError();
        return clone;
    }

    public bool Compile(string source, bool optionExplicit = true, bool allowExternal = true)
    {
        ErrorObject = new InterpreterError();
        var sourceStream = new StringInputStream();
        var parser = new SyntaxAnalyser(ErrorObject);
        code = new List<object>();
        parser.Parse(sourceStream.Connect(source), this, optionExplicit, allowExternal);
        return ErrorObject.Number == 0;
    }

    public void Dispose()
    {
        ErrorObject = null;
    }

    public Identifier ImportAdd(string name, ScriptValue value = default, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.IdVariable)
    {
        return external.Allocate(name, value, idType);
    }

    public void ImportClear()
    {
        external = new Scope();
    }

    public void ImportItem(string name, ScriptValue value)
    {
        external.Assign(name, value);
    }

    public ScriptValue ImportRead(string name)
    {
        return external.Retrieve(name)?.Value ?? ScriptValue.Null;
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
        var operation = new object[parameters.Length + 1];
        operation[0] = opCode;
        for (int i = 0; i < parameters.Length; i++)
        {
            operation[i + 1] = parameters[i];
        }
        code.Add(operation);
        return code.Count;
    }

    internal void CloneAdd(object value) => code.Add(value);

    internal object GetInstruction(int index) => code[index];

    internal Scope External() => external;

    internal void FixUp(int index, params object[] parameters)
    {
        var operation = new object[parameters.Length + 1];
        var tmp = (object[])code[index];
        operation[0] = tmp[0];
        for (int i = 0; i < parameters.Length; i++)
        {
            operation[i + 1] = parameters[i];
        }
        code.RemoveAt(index);
        if (index > code.Count)
            code.Add(operation);
        else
            code.Insert(index, operation);
    }

    private void BinaryMathOperators(Opcodes opcode)
    {
        var register = scopes!.PopScopes();
        var accumulator = scopes.PopScopes();

        try
        {
            ScriptValue result = opcode switch
            {
                Opcodes.Add => Helper.ExtractDouble(accumulator) + Helper.ExtractDouble(register),
                Opcodes.Sub => Helper.ExtractDouble(accumulator) - Helper.ExtractDouble(register),
                Opcodes.Multiplication => Helper.ExtractDouble(accumulator) * Helper.ExtractDouble(register),
                Opcodes.Division => Helper.ExtractDouble(accumulator) / Helper.ExtractDouble(register),
                Opcodes.Div => Helper.ExtractInt(accumulator) / Helper.ExtractInt(register),
                Opcodes.Mod => Helper.ExtractDouble(accumulator) % Helper.ExtractDouble(register),
                Opcodes.Power => Math.Pow(Helper.ExtractDouble(accumulator), Helper.ExtractDouble(register)),
                Opcodes.StringConcat => Helper.ExtractString(accumulator) + Helper.ExtractString(register),
                Opcodes.Or => Helper.ExtractInt(accumulator) | Helper.ExtractInt(register),
                Opcodes.And => Helper.ExtractInt(accumulator) & Helper.ExtractInt(register),
                Opcodes.Eq => Helper.ExtractString(accumulator).Equals(Helper.ExtractString(register)),
                Opcodes.NotEq => !Helper.ExtractString(accumulator).Equals(Helper.ExtractString(register)),
                Opcodes.Lt => Helper.ExtractDouble(accumulator) < Helper.ExtractDouble(register),
                Opcodes.LEq => Helper.ExtractDouble(accumulator) <= Helper.ExtractDouble(register),
                Opcodes.Gt => Helper.ExtractDouble(accumulator) > Helper.ExtractDouble(register),
                Opcodes.GEq => Helper.ExtractDouble(accumulator) >= Helper.ExtractDouble(register),
                _ => ScriptValue.Null
            };
            scopes.Push(result);
        }
        catch (Exception ex)
        {
            running = false;
            ErrorObject!.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run",
                $"Error during calculation (binary op {opcode}): {ex.HResult}({ex.Message})", 0, 0, 0);
        }
    }

    private static int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);

    private void Interpret(CancellationToken cancellationToken)
    {
        scopes = new Scopes();
        scopes.PushScope(external);
        scopes.PushScope();

        Cancel = false;
        running = true;
        recursionDepth = 0;
        pc = 0;

        while (pc < code.Count && running)
        {
            var operation = (object[])code[pc];
            var opcode = (Opcodes)operation[0];

            switch (opcode)
            {
                case Opcodes.AllocConst:
                    scopes.Allocate(operation[1]!.ToString()!, ScriptValue.FromObject(operation[2]), Identifier.IdentifierTypes.IdConst);
                    break;

                case Opcodes.AllocVar:
                    scopes.Allocate(operation[1]!.ToString()!);
                    break;

                case Opcodes.PushValue:
                    scopes.Push(ScriptValue.FromObject(operation[1]));
                    break;

                case Opcodes.PushVariable:
                    ExecutePushVariable(operation);
                    break;

                case Opcodes.Pop:
                    scopes.PopScopes();
                    break;

                case Opcodes.PopWithIndex:
                    {
                        var register = scopes.Pop(Convert.ToInt32(operation[1]));
                        scopes.Push(register?.Value ?? ScriptValue.Null);
                    }
                    break;

                case Opcodes.Assign:
                    ExecuteAssign(operation);
                    break;

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
                    BinaryMathOperators(opcode);
                    break;

                case Opcodes.Negate:
                case Opcodes.Not:
                case Opcodes.Factorial:
                case Opcodes.Sin:
                case Opcodes.Cos:
                case Opcodes.Tan:
                case Opcodes.ATan:
                    UnaryMathOperators(opcode);
                    break;

                case Opcodes.DebugPrint:
                    {
                        var register = scopes.PopScopes();
                        DebugPrint?.Invoke(register.Value.AsString());
                    }
                    break;

                case Opcodes.DebugClear:
                    DebugClear?.Invoke();
                    break;

                case Opcodes.DebugShow:
                    DebugShow?.Invoke();
                    break;

                case Opcodes.DebugHide:
                    DebugHide?.Invoke();
                    break;

                case Opcodes.Message:
                    ExecuteMessage();
                    break;

                case Opcodes.Msgbox:
                    if (!AllowUi)
                    {
                        running = false;
                        ErrorObject!.Raise((int)InterpreterError.RunErrors.errNoUIallowed, "Code.Run",
                            "MsgBox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                    }
                    break;

                case Opcodes.DoEvents:
                    break;

                case Opcodes.Inputbox:
                    if (!AllowUi)
                    {
                        running = false;
                        ErrorObject!.Raise((int)InterpreterError.RunErrors.errNoUIallowed, "Code.Run",
                            "Inputbox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                    }
                    break;

                case Opcodes.Jump:
                    pc = Convert.ToInt32(operation[1]) - 1;
                    break;

                case Opcodes.JumpTrue:
                case Opcodes.JumpFalse:
                    {
                        var value = scopes.PopScopes().Value;
                        if (!value.AsBoolean())
                        {
                            pc = Convert.ToInt32(operation[1]) - 1;
                        }
                    }
                    break;

                case Opcodes.JumpPop:
                    pc = scopes.PopScopes().Value.AsInt() - 1;
                    break;

                case Opcodes.PushScope:
                    scopes.PushScope();
                    break;

                case Opcodes.PopScope:
                    scopes.PopScopes();
                    break;

                case Opcodes.Call:
                    if (++recursionDepth > MaxRecursionDepth)
                    {
                        running = false;
                        throw new ScriptTooComplexException($"Maximum recursion depth ({MaxRecursionDepth}) exceeded");
                    }
                    scopes.Allocate("~RETURNADDR", ScriptValue.FromInt(pc + 1), Identifier.IdentifierTypes.IdConst);
                    pc = Convert.ToInt32(operation[1]) - 1;
                    break;

                case Opcodes.Return:
                    recursionDepth--;
                    pc = scopes.Retrieve("~RETURNADDR")!.Value.AsInt() - 1;
                    break;
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

    private void ExecutePushVariable(object[] operation)
    {
        try
        {
            var tmp = operation[1]!;
            var name = tmp is Identifier id ? id.Value.AsString() : tmp.ToString();
            var register = scopes!.Retrieve(name!);

            if (register == null)
            {
                running = false;
                ErrorObject!.Raise((int)InterpreterError.RunErrors.errUninitializedVar, "Code.Run",
                    $"Variable '{operation[1]}' hasn't been assigned a value yet", 0, 0, 0);
                return;
            }

            scopes.Push(register.Value);
        }
        catch
        {
            running = false;
            ErrorObject!.Raise((int)InterpreterError.RunErrors.errUnknownVar, "Code.Run",
                $"Unknown variable '{operation[1]}'", 0, 0, 0);
        }
    }

    private void ExecuteAssign(object[] operation)
    {
        var register = scopes!.Pop();
        var value = register?.Value ?? ScriptValue.Null;

        if (!scopes.Assign(operation[1]!.ToString()!, value))
        {
            var accepted = false;
            Assign?.Invoke(operation[1]!.ToString()!, value.AsString(), ref accepted);
            if (!accepted)
            {
                scopes.Allocate(operation[1]!.ToString()!, value);
            }
        }
    }

    private void ExecuteMessage()
    {
        try
        {
            var register = scopes!.PopScopes();
            var akkumulator = scopes.PopScopes();

            var msg = register.Value.AsString();
            var type = akkumulator.Value.AsInt();

            Message?.Invoke(type, msg);
        }
        catch
        {
            Message?.Invoke(-1, string.Empty);
        }
    }

    private void UnaryMathOperators(Opcodes opcode)
    {
        var akkumulator = scopes!.PopScopes().Value;

        try
        {
            var number = akkumulator.AsDouble();

            ScriptValue result = opcode switch
            {
                Opcodes.Negate => -number,
                Opcodes.Not => !akkumulator.AsBoolean(),
                Opcodes.Factorial => Factorial((int)number),
                Opcodes.Sin => Math.Sin(number),
                Opcodes.Cos => Math.Cos(number),
                Opcodes.Tan => Math.Tan(number),
                Opcodes.ATan => Math.Atan(number),
                _ => ScriptValue.Null
            };

            scopes.Push(result);
        }
        catch (Exception ex)
        {
            running = false;
            ErrorObject?.Raise((int)InterpreterError.RunErrors.errMath, "Code.Run",
                $"Error during calculation (unary op {opcode}): {ex.HResult} ({ex.Message})", 0, 0, 0);
        }
    }
}
