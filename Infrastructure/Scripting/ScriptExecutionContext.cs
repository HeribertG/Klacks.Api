namespace Klacks.Api.Infrastructure.Scripting;

public sealed class ScriptExecutionContext
{
    private const int MaxRecursionDepth = 1000;

    private readonly CompiledScript script;
    private readonly List<ResultMessage> messages = new();

    private Scopes? scopes;
    private int pc;
    private int recursionDepth;
    private bool running;
    private InterpreterError? errorObject;

    public event Code.MessageEventHandler? Message;
    public event Code.DebugPrintEventHandler? DebugPrint;
    public event Code.DebugClearEventHandler? DebugClear;
    public event Code.DebugShowEventHandler? DebugShow;
    public event Code.DebugHideEventHandler? DebugHide;

    public bool AllowUi { get; set; }

    public ScriptExecutionContext(CompiledScript script)
    {
        this.script = script ?? throw new ArgumentNullException(nameof(script));
    }

    public ScriptResult Execute(CancellationToken cancellationToken = default)
    {
        if (script.HasError)
        {
            return ScriptResult.Fail(script.Error!);
        }

        messages.Clear();
        errorObject = new InterpreterError();

        try
        {
            Interpret(cancellationToken);

            if (errorObject.Number != 0)
            {
                return ScriptResult.Fail(
                    new ScriptError(errorObject.Number, errorObject.Description, errorObject.Line, errorObject.Col),
                    messages
                );
            }

            return ScriptResult.Ok(messages);
        }
        catch (ScriptTooComplexException ex)
        {
            return ScriptResult.Fail(new ScriptError(-1, ex.Message, 0, 0), messages);
        }
    }

    private void Interpret(CancellationToken cancellationToken)
    {
        scopes = new Scopes();

        var externalScope = new Scope();
        foreach (var kvp in script.ExternalSymbols)
        {
            externalScope.Allocate(kvp.Key, kvp.Value.Value, kvp.Value.IdType);
        }
        scopes.PushScope(externalScope);
        scopes.PushScope();

        running = true;
        recursionDepth = 0;
        pc = 0;

        var instructions = script.Instructions;

        while (pc < instructions.Count && running)
        {
            var operation = instructions[pc];
            ExecuteInstruction(operation);

            pc++;

            if (cancellationToken.IsCancellationRequested)
            {
                running = false;
                errorObject!.Raise(
                    (int)InterpreterError.RunErrors.errCancelled,
                    "ScriptExecutionContext.Execute",
                    "Script execution cancelled",
                    0, 0, 0
                );
            }
        }

        running = false;
    }

    private void ExecuteInstruction(object[] operation)
    {
        var opcode = (Opcodes)operation[0];

        switch (opcode)
        {
            case Opcodes.AllocConst:
                scopes!.Allocate(operation[1]!.ToString()!, ScriptValue.FromObject(operation[2]), Identifier.IdentifierTypes.IdConst);
                break;

            case Opcodes.AllocVar:
                scopes!.Allocate(operation[1]!.ToString()!);
                break;

            case Opcodes.PushValue:
                scopes!.Push(ScriptValue.FromObject(operation[1]));
                break;

            case Opcodes.PushVariable:
                ExecutePushVariable(operation);
                break;

            case Opcodes.Pop:
                scopes!.PopScopes();
                break;

            case Opcodes.PopWithIndex:
                {
                    var register = scopes!.Pop(Convert.ToInt32(operation[1]));
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
                ExecuteBinaryOp(opcode);
                break;

            case Opcodes.Negate:
            case Opcodes.Not:
            case Opcodes.Factorial:
            case Opcodes.Sin:
            case Opcodes.Cos:
            case Opcodes.Tan:
            case Opcodes.ATan:
                ExecuteUnaryOp(opcode);
                break;

            case Opcodes.DebugPrint:
                {
                    var register = scopes!.PopScopes();
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
            case Opcodes.Inputbox:
                if (!AllowUi)
                {
                    running = false;
                    errorObject!.Raise(
                        (int)InterpreterError.RunErrors.errNoUIallowed,
                        "ScriptExecutionContext.Execute",
                        "UI statements not allowed in this context",
                        0, 0, 0
                    );
                }
                break;

            case Opcodes.DoEvents:
                break;

            case Opcodes.Jump:
                pc = Convert.ToInt32(operation[1]) - 1;
                break;

            case Opcodes.JumpTrue:
            case Opcodes.JumpFalse:
                {
                    var value = scopes!.PopScopes().Value;
                    if (!value.AsBoolean())
                    {
                        pc = Convert.ToInt32(operation[1]) - 1;
                    }
                }
                break;

            case Opcodes.JumpPop:
                pc = scopes!.PopScopes().Value.AsInt() - 1;
                break;

            case Opcodes.PushScope:
                scopes!.PushScope();
                break;

            case Opcodes.PopScope:
                scopes!.PopScopes();
                break;

            case Opcodes.Call:
                if (++recursionDepth > MaxRecursionDepth)
                {
                    running = false;
                    throw new ScriptTooComplexException($"Maximum recursion depth ({MaxRecursionDepth}) exceeded");
                }
                scopes!.Allocate("~RETURNADDR", ScriptValue.FromInt(pc + 1), Identifier.IdentifierTypes.IdConst);
                pc = Convert.ToInt32(operation[1]) - 1;
                break;

            case Opcodes.Return:
                recursionDepth--;
                pc = scopes!.Retrieve("~RETURNADDR")!.Value.AsInt() - 1;
                break;
        }
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
                errorObject!.Raise(
                    (int)InterpreterError.RunErrors.errUninitializedVar,
                    "ScriptExecutionContext.Execute",
                    $"Variable '{operation[1]}' has not been assigned a value",
                    0, 0, 0
                );
                return;
            }

            scopes.Push(register.Value);
        }
        catch
        {
            running = false;
            errorObject!.Raise(
                (int)InterpreterError.RunErrors.errUnknownVar,
                "ScriptExecutionContext.Execute",
                $"Unknown variable '{operation[1]}'",
                0, 0, 0
            );
        }
    }

    private void ExecuteAssign(object[] operation)
    {
        var register = scopes!.Pop();
        var value = register?.Value ?? ScriptValue.Null;

        if (!scopes.Assign(operation[1]!.ToString()!, value))
        {
            scopes.Allocate(operation[1]!.ToString()!, value);
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

            if (!string.IsNullOrEmpty(msg) && type > 0)
            {
                messages.Add(new ResultMessage { Type = type, Message = msg });
            }
        }
        catch
        {
            Message?.Invoke(-1, string.Empty);
        }
    }

    private void ExecuteBinaryOp(Opcodes opcode)
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
            errorObject!.Raise(
                (int)InterpreterError.RunErrors.errMath,
                "ScriptExecutionContext.Execute",
                $"Error during calculation (binary op {opcode}): {ex.Message}",
                0, 0, 0
            );
        }
    }

    private void ExecuteUnaryOp(Opcodes opcode)
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
            errorObject!.Raise(
                (int)InterpreterError.RunErrors.errMath,
                "ScriptExecutionContext.Execute",
                $"Error during calculation (unary op {opcode}): {ex.Message}",
                0, 0, 0
            );
        }
    }

    private static int Factorial(int n)
    {
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }
}
