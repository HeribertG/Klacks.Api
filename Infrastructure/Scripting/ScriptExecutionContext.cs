namespace Klacks.Api.Infrastructure.Scripting;

public sealed class ScriptExecutionContext
{
    public delegate void AssignEventHandler(string name, string value, ref bool accepted);
    public delegate void DebugClearEventHandler();
    public delegate void DebugHideEventHandler();
    public delegate void DebugPrintEventHandler(string msg);
    public delegate void DebugShowEventHandler();
    public delegate void MessageEventHandler(int type, string message);
    public delegate void RetrieveEventHandler(string name, string value, bool accepted);

    private const int MaxRecursionDepth = 1000;
    private const int MaxInstructions = 1_000_000;

    private readonly CompiledScript script;
    private readonly List<ResultMessage> messages = new();
    private readonly Random _random = new();

    private Scopes? scopes;
    private int pc;
    private int recursionDepth;
    private bool running;
    private InterpreterError? errorObject;

    public event MessageEventHandler? Message;
    public event DebugPrintEventHandler? DebugPrint;
    public event DebugClearEventHandler? DebugClear;
    public event DebugShowEventHandler? DebugShow;
    public event DebugHideEventHandler? DebugHide;

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
        var instructionCount = 0;

        while (pc < instructions.Count && running)
        {
            var operation = instructions[pc];
            ExecuteInstruction(operation);

            pc++;
            instructionCount++;

            if (instructionCount > MaxInstructions)
            {
                running = false;
                throw new ScriptTooComplexException($"Maximum instruction count ({MaxInstructions}) exceeded");
            }

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
            case Opcodes.Abs:
            case Opcodes.Sqr:
            case Opcodes.Log:
            case Opcodes.Exp:
            case Opcodes.Sgn:
                ExecuteUnaryOp(opcode);
                break;

            // Time Functions
            case Opcodes.TimeToHours:
                ExecuteTimeToHours();
                break;
            case Opcodes.TimeOverlap:
                ExecuteTimeOverlap();
                break;

            // String Functions
            case Opcodes.Len:
                ExecuteLen();
                break;
            case Opcodes.Left:
                ExecuteLeft();
                break;
            case Opcodes.Right:
                ExecuteRight();
                break;
            case Opcodes.Mid:
                ExecuteMid();
                break;
            case Opcodes.InStr:
                ExecuteInStr();
                break;
            case Opcodes.Replace:
                ExecuteReplace();
                break;
            case Opcodes.Trim:
                ExecuteTrim();
                break;
            case Opcodes.UCase:
                ExecuteUCase();
                break;
            case Opcodes.LCase:
                ExecuteLCase();
                break;

            // Math Functions
            case Opcodes.Rnd:
                scopes!.Push(ScriptValue.FromNumber(_random.NextDouble()));
                break;
            case Opcodes.Round:
                ExecuteRound();
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
                if (operation.Length < 2)
                {
                    errorObject!.Raise(999, "ScriptExecutionContext", $"Malformed Jump instruction at PC {pc}: missing target address", 0, 0, 0);
                    running = false;
                    return;
                }
                pc = Convert.ToInt32(operation[1]) - 1;
                break;

            case Opcodes.JumpTrue:
                {
                    var value = scopes!.PopScopes().Value;
                    if (value.AsBoolean())
                    {
                        if (operation.Length < 2)
                        {
                            errorObject!.Raise(999, "ScriptExecutionContext", $"Malformed JumpTrue instruction at PC {pc}: missing target address", 0, 0, 0);
                            running = false;
                            return;
                        }
                        pc = Convert.ToInt32(operation[1]) - 1;
                    }
                }
                break;

            case Opcodes.JumpFalse:
                {
                    var value = scopes!.PopScopes().Value;
                    if (!value.AsBoolean())
                    {
                        if (operation.Length < 2)
                        {
                            errorObject!.Raise(999, "ScriptExecutionContext", $"Malformed JumpFalse instruction at PC {pc}: missing target address", 0, 0, 0);
                            running = false;
                            return;
                        }
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
                scopes!.PopScope();
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
                Opcodes.Abs => Math.Abs(number),
                Opcodes.Sqr => Math.Sqrt(number),
                Opcodes.Log => Math.Log(number),
                Opcodes.Exp => Math.Exp(number),
                Opcodes.Sgn => Math.Sign(number),
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

    private void ExecuteLen()
    {
        var str = scopes!.PopScopes().Value.AsString();
        scopes.Push(ScriptValue.FromInt(str.Length));
    }

    private void ExecuteLeft()
    {
        var length = scopes!.PopScopes().Value.AsInt();
        var str = scopes.PopScopes().Value.AsString();
        var result = length >= str.Length ? str : str[..length];
        scopes.Push(ScriptValue.FromString(result));
    }

    private void ExecuteRight()
    {
        var length = scopes!.PopScopes().Value.AsInt();
        var str = scopes.PopScopes().Value.AsString();
        var result = length >= str.Length ? str : str[^length..];
        scopes.Push(ScriptValue.FromString(result));
    }

    private void ExecuteMid()
    {
        var length = scopes!.PopScopes().Value.AsInt();
        var start = scopes.PopScopes().Value.AsInt() - 1;
        var str = scopes.PopScopes().Value.AsString();
        if (start < 0) start = 0;
        if (start >= str.Length)
        {
            scopes.Push(ScriptValue.FromString(string.Empty));
            return;
        }
        var actualLength = Math.Min(length, str.Length - start);
        scopes.Push(ScriptValue.FromString(str.Substring(start, actualLength)));
    }

    private void ExecuteInStr()
    {
        var search = scopes!.PopScopes().Value.AsString();
        var str = scopes.PopScopes().Value.AsString();
        var index = str.IndexOf(search, StringComparison.Ordinal);
        scopes.Push(ScriptValue.FromInt(index + 1));
    }

    private void ExecuteReplace()
    {
        var replacement = scopes!.PopScopes().Value.AsString();
        var search = scopes.PopScopes().Value.AsString();
        var str = scopes.PopScopes().Value.AsString();
        scopes.Push(ScriptValue.FromString(str.Replace(search, replacement)));
    }

    private void ExecuteTrim()
    {
        var str = scopes!.PopScopes().Value.AsString();
        scopes.Push(ScriptValue.FromString(str.Trim()));
    }

    private void ExecuteUCase()
    {
        var str = scopes!.PopScopes().Value.AsString();
        scopes.Push(ScriptValue.FromString(str.ToUpperInvariant()));
    }

    private void ExecuteLCase()
    {
        var str = scopes!.PopScopes().Value.AsString();
        scopes.Push(ScriptValue.FromString(str.ToLowerInvariant()));
    }

    private void ExecuteRound()
    {
        var decimals = scopes!.PopScopes().Value.AsInt();
        var number = scopes.PopScopes().Value.AsDouble();
        scopes.Push(ScriptValue.FromNumber(Math.Round(number, decimals, MidpointRounding.AwayFromZero)));
    }

    private void ExecuteTimeToHours()
    {
        var timeStr = scopes!.PopScopes().Value.AsString();
        var parts = timeStr.Split(':');
        if (parts.Length >= 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
        {
            var decimalHours = hours + (minutes / 60.0);
            scopes.Push(ScriptValue.FromNumber(decimalHours));
        }
        else
        {
            scopes.Push(ScriptValue.FromNumber(0));
        }
    }

    private void ExecuteTimeOverlap()
    {
        var inputEndStr = scopes!.PopScopes().Value.AsString();
        var inputStartStr = scopes.PopScopes().Value.AsString();
        var segmentEndStr = scopes.PopScopes().Value.AsString();
        var segmentStartStr = scopes.PopScopes().Value.AsString();

        var segmentStart = ParseTimeToMinutes(segmentStartStr);
        var segmentEnd = ParseTimeToMinutes(segmentEndStr);
        var inputStart = ParseTimeToMinutes(inputStartStr);
        var inputEnd = ParseTimeToMinutes(inputEndStr);

        if (segmentStart < 0 || segmentEnd < 0 || inputStart < 0 || inputEnd < 0)
        {
            scopes.Push(ScriptValue.FromNumber(0));
            return;
        }

        var overlapMinutes = CalculateOverlapMinutes(segmentStart, segmentEnd, inputStart, inputEnd);
        scopes.Push(ScriptValue.FromNumber(overlapMinutes / 60.0));
    }

    private static int ParseTimeToMinutes(string timeStr)
    {
        var parts = timeStr.Split(':');
        if (parts.Length >= 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
        {
            return hours * 60 + minutes;
        }
        return -1;
    }

    private static int CalculateOverlapMinutes(int segStart, int segEnd, int inStart, int inEnd)
    {
        const int dayMinutes = 24 * 60;
        var segCrossesMidnight = segEnd <= segStart;
        var inCrossesMidnight = inEnd <= inStart;

        if (segCrossesMidnight)
        {
            segEnd += dayMinutes;
        }
        if (inCrossesMidnight)
        {
            inEnd += dayMinutes;
        }

        var overlap = CalculateSimpleOverlap(segStart, segEnd, inStart, inEnd);

        if (segCrossesMidnight && !inCrossesMidnight)
        {
            overlap += CalculateSimpleOverlap(segStart - dayMinutes, segEnd - dayMinutes, inStart, inEnd);
        }
        if (inCrossesMidnight && !segCrossesMidnight)
        {
            overlap += CalculateSimpleOverlap(segStart, segEnd, inStart - dayMinutes, inEnd - dayMinutes);
        }

        return overlap;
    }

    private static int CalculateSimpleOverlap(int start1, int end1, int start2, int end2)
    {
        var overlapStart = Math.Max(start1, start2);
        var overlapEnd = Math.Min(end1, end2);
        return Math.Max(0, overlapEnd - overlapStart);
    }
}
