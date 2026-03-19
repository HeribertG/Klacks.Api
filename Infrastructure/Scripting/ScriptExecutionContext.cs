// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly Dictionary<Opcodes, Action<object[]>> _instructionHandlers;

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
        _instructionHandlers = BuildInstructionHandlers();
    }

    private Dictionary<Opcodes, Action<object[]>> BuildInstructionHandlers()
    {
        return new Dictionary<Opcodes, Action<object[]>>
        {
            [Opcodes.AllocConst] = op => ExecuteVariableAndStackOps(Opcodes.AllocConst, op),
            [Opcodes.AllocVar] = op => ExecuteVariableAndStackOps(Opcodes.AllocVar, op),
            [Opcodes.PushValue] = op => ExecuteVariableAndStackOps(Opcodes.PushValue, op),
            [Opcodes.PushVariable] = op => ExecuteVariableAndStackOps(Opcodes.PushVariable, op),
            [Opcodes.Pop] = op => ExecuteVariableAndStackOps(Opcodes.Pop, op),
            [Opcodes.PopWithIndex] = op => ExecuteVariableAndStackOps(Opcodes.PopWithIndex, op),
            [Opcodes.Assign] = op => ExecuteVariableAndStackOps(Opcodes.Assign, op),
            [Opcodes.PushScope] = op => ExecuteVariableAndStackOps(Opcodes.PushScope, op),
            [Opcodes.PopScope] = op => ExecuteVariableAndStackOps(Opcodes.PopScope, op),

            [Opcodes.Add] = _ => ExecuteBinaryOp(Opcodes.Add),
            [Opcodes.Sub] = _ => ExecuteBinaryOp(Opcodes.Sub),
            [Opcodes.Multiplication] = _ => ExecuteBinaryOp(Opcodes.Multiplication),
            [Opcodes.Division] = _ => ExecuteBinaryOp(Opcodes.Division),
            [Opcodes.Div] = _ => ExecuteBinaryOp(Opcodes.Div),
            [Opcodes.Mod] = _ => ExecuteBinaryOp(Opcodes.Mod),
            [Opcodes.Power] = _ => ExecuteBinaryOp(Opcodes.Power),
            [Opcodes.StringConcat] = _ => ExecuteBinaryOp(Opcodes.StringConcat),
            [Opcodes.Or] = _ => ExecuteBinaryOp(Opcodes.Or),
            [Opcodes.And] = _ => ExecuteBinaryOp(Opcodes.And),
            [Opcodes.Eq] = _ => ExecuteBinaryOp(Opcodes.Eq),
            [Opcodes.NotEq] = _ => ExecuteBinaryOp(Opcodes.NotEq),
            [Opcodes.Lt] = _ => ExecuteBinaryOp(Opcodes.Lt),
            [Opcodes.LEq] = _ => ExecuteBinaryOp(Opcodes.LEq),
            [Opcodes.Gt] = _ => ExecuteBinaryOp(Opcodes.Gt),
            [Opcodes.GEq] = _ => ExecuteBinaryOp(Opcodes.GEq),

            [Opcodes.Negate] = _ => ExecuteUnaryOp(Opcodes.Negate),
            [Opcodes.Not] = _ => ExecuteUnaryOp(Opcodes.Not),
            [Opcodes.Factorial] = _ => ExecuteUnaryOp(Opcodes.Factorial),
            [Opcodes.Sin] = _ => ExecuteUnaryOp(Opcodes.Sin),
            [Opcodes.Cos] = _ => ExecuteUnaryOp(Opcodes.Cos),
            [Opcodes.Tan] = _ => ExecuteUnaryOp(Opcodes.Tan),
            [Opcodes.ATan] = _ => ExecuteUnaryOp(Opcodes.ATan),
            [Opcodes.Abs] = _ => ExecuteUnaryOp(Opcodes.Abs),
            [Opcodes.Sqr] = _ => ExecuteUnaryOp(Opcodes.Sqr),
            [Opcodes.Log] = _ => ExecuteUnaryOp(Opcodes.Log),
            [Opcodes.Exp] = _ => ExecuteUnaryOp(Opcodes.Exp),
            [Opcodes.Sgn] = _ => ExecuteUnaryOp(Opcodes.Sgn),

            [Opcodes.Jump] = op => ExecuteJumpOps(Opcodes.Jump, op),
            [Opcodes.JumpTrue] = op => ExecuteJumpOps(Opcodes.JumpTrue, op),
            [Opcodes.JumpFalse] = op => ExecuteJumpOps(Opcodes.JumpFalse, op),
            [Opcodes.JumpPop] = op => ExecuteJumpOps(Opcodes.JumpPop, op),

            [Opcodes.Call] = op => ExecuteCallReturn(Opcodes.Call, op),
            [Opcodes.Return] = op => ExecuteCallReturn(Opcodes.Return, op),

            [Opcodes.TimeToHours] = _ => ExecuteTimeToHours(),
            [Opcodes.TimeOverlap] = _ => ExecuteTimeOverlap(),

            [Opcodes.Len] = _ => ExecuteLen(),
            [Opcodes.Left] = _ => ExecuteLeft(),
            [Opcodes.Right] = _ => ExecuteRight(),
            [Opcodes.Mid] = _ => ExecuteMid(),
            [Opcodes.InStr] = _ => ExecuteInStr(),
            [Opcodes.Replace] = _ => ExecuteReplace(),
            [Opcodes.Trim] = _ => ExecuteTrim(),
            [Opcodes.UCase] = _ => ExecuteUCase(),
            [Opcodes.LCase] = _ => ExecuteLCase(),

            [Opcodes.Rnd] = _ => scopes!.Push(ScriptValue.FromNumber(_random.NextDouble())),
            [Opcodes.Round] = _ => ExecuteRound(),

            [Opcodes.DebugPrint] = _ => ExecuteDebugOps(Opcodes.DebugPrint),
            [Opcodes.DebugClear] = _ => ExecuteDebugOps(Opcodes.DebugClear),
            [Opcodes.DebugShow] = _ => ExecuteDebugOps(Opcodes.DebugShow),
            [Opcodes.DebugHide] = _ => ExecuteDebugOps(Opcodes.DebugHide),

            [Opcodes.Message] = _ => ExecuteMessage(),

            [Opcodes.Msgbox] = _ => ExecuteUiOps(),
            [Opcodes.Inputbox] = _ => ExecuteUiOps(),

            [Opcodes.DoEvents] = _ => { }
        };
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
                errorObject!.Raise(new InterpreterErrorInfo(
                    (int)InterpreterError.RunErrors.errCancelled,
                    "ScriptExecutionContext.Execute",
                    "Script execution cancelled",
                    0, 0, 0));
            }
        }

        running = false;
    }

    private void ExecuteInstruction(object[] operation)
    {
        var opcode = (Opcodes)operation[0];

        if (_instructionHandlers.TryGetValue(opcode, out var handler))
            handler(operation);
    }

    private void ExecuteVariableAndStackOps(Opcodes opcode, object[] operation)
    {
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
                var register = scopes!.Pop(Convert.ToInt32(operation[1]));
                scopes.Push(register?.Value ?? ScriptValue.Null);
                break;

            case Opcodes.Assign:
                ExecuteAssign(operation);
                break;

            case Opcodes.PushScope:
                scopes!.PushScope();
                break;

            case Opcodes.PopScope:
                scopes!.PopScope();
                break;
        }
    }

    private void ExecuteJumpOps(Opcodes opcode, object[] operation)
    {
        switch (opcode)
        {
            case Opcodes.Jump:
                if (operation.Length < 2)
                {
                    errorObject!.Raise(new InterpreterErrorInfo(999, "ScriptExecutionContext", $"Malformed Jump instruction at PC {pc}: missing target address", 0, 0, 0));
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
                            errorObject!.Raise(new InterpreterErrorInfo(999, "ScriptExecutionContext", $"Malformed JumpTrue instruction at PC {pc}: missing target address", 0, 0, 0));
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
                            errorObject!.Raise(new InterpreterErrorInfo(999, "ScriptExecutionContext", $"Malformed JumpFalse instruction at PC {pc}: missing target address", 0, 0, 0));
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
        }
    }

    private void ExecuteCallReturn(Opcodes opcode, object[] operation)
    {
        switch (opcode)
        {
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

    private void ExecuteDebugOps(Opcodes opcode)
    {
        switch (opcode)
        {
            case Opcodes.DebugPrint:
                var register = scopes!.PopScopes();
                DebugPrint?.Invoke(register.Value.AsString());
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
        }
    }

    private void ExecuteUiOps()
    {
        if (!AllowUi)
        {
            running = false;
            errorObject!.Raise(new InterpreterErrorInfo(
                (int)InterpreterError.RunErrors.errNoUIallowed,
                "ScriptExecutionContext.Execute",
                "UI statements not allowed in this context",
                0, 0, 0));
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
                errorObject!.Raise(new InterpreterErrorInfo(
                    (int)InterpreterError.RunErrors.errUninitializedVar,
                    "ScriptExecutionContext.Execute",
                    $"Variable '{operation[1]}' has not been assigned a value",
                    0, 0, 0));
                return;
            }

            scopes.Push(register.Value);
        }
        catch
        {
            running = false;
            errorObject!.Raise(new InterpreterErrorInfo(
                (int)InterpreterError.RunErrors.errUnknownVar,
                "ScriptExecutionContext.Execute",
                $"Unknown variable '{operation[1]}'",
                0, 0, 0));
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
                Opcodes.Eq => Helper.AreEqual(accumulator, register),
                Opcodes.NotEq => !Helper.AreEqual(accumulator, register),
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
            errorObject!.Raise(new InterpreterErrorInfo(
                (int)InterpreterError.RunErrors.errMath,
                "ScriptExecutionContext.Execute",
                $"Error during calculation (binary op {opcode}): {ex.Message}",
                0, 0, 0));
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
            errorObject!.Raise(new InterpreterErrorInfo(
                (int)InterpreterError.RunErrors.errMath,
                "ScriptExecutionContext.Execute",
                $"Error during calculation (unary op {opcode}): {ex.Message}",
                0, 0, 0));
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
