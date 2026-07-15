// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Scripting;

/// <summary>
/// Immutable result of parsing a macro script: instruction list, external symbols and optional compile error.
/// </summary>
/// <param name="instructions">Opcode instructions produced by the parser</param>
/// <param name="externalSymbols">IMPORT symbols that callers fill via SetExternalValue before execution</param>
/// <param name="error">Compile error, or null when parsing succeeded</param>
public sealed class CompiledScript
{
    private const int CompileTimeoutMs = 5000;

    private readonly List<object[]> instructions;
    private readonly Dictionary<string, Identifier> externalSymbols;

    private CompiledScript(List<object[]> instructions, Dictionary<string, Identifier> externalSymbols, ScriptError? error)
    {
        this.instructions = instructions;
        this.externalSymbols = externalSymbols;
        Error = error;
    }

    public IReadOnlyList<object[]> Instructions => instructions;
    public IReadOnlyDictionary<string, Identifier> ExternalSymbols => externalSymbols;
    public ScriptError? Error { get; }
    public bool HasError => Error != null;

    public static CompiledScript Compile(string source, bool optionExplicit = true, bool allowExternal = true)
    {
        var compileTask = Task.Run(() => CompileCore(source, optionExplicit, allowExternal));
        if (Task.WaitAny(new Task[] { compileTask }, CompileTimeoutMs) < 0)
        {
            var timeoutError = new ScriptError(
                (int)InterpreterError.RunErrors.errTimedOut,
                $"Script compilation did not finish within {CompileTimeoutMs} ms",
                0,
                0);
            return new CompiledScript(
                new List<object[]>(),
                new Dictionary<string, Identifier>(StringComparer.OrdinalIgnoreCase),
                timeoutError);
        }

        return compileTask.GetAwaiter().GetResult();
    }

    private static CompiledScript CompileCore(string source, bool optionExplicit, bool allowExternal)
    {
        var errorObject = new InterpreterError();
        var sourceStream = new StringInputStream();
        var parser = new SyntaxAnalyser(errorObject);

        var code = new Code();
        parser.Parse(sourceStream.Connect(source), code, optionExplicit, allowExternal);

        var parseError = code.ErrorObject ?? errorObject;

        ScriptError? error = null;
        if (parseError.Number != 0)
        {
            error = new ScriptError(
                parseError.Number,
                parseError.Description,
                parseError.Line,
                parseError.Col
            );
        }

        var instructions = new List<object[]>();
        for (int i = 0; i < code.Count; i++)
        {
            instructions.Add((object[])code.GetInstruction(i));
        }

        var externalSymbols = new Dictionary<string, Identifier>(StringComparer.OrdinalIgnoreCase);
        var external = code.External();
        for (int i = 0; i < external.CloneCount(); i++)
        {
            var id = (Identifier)external.CloneItem(i);
            if (id.Name != null)
            {
                externalSymbols[id.Name] = id;
            }
        }

        return new CompiledScript(instructions, externalSymbols, error);
    }

    public CompiledScript CloneForExecution()
    {
        var clonedSymbols = new Dictionary<string, Identifier>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in externalSymbols)
        {
            clonedSymbols[kvp.Key] = new Identifier
            {
                Name = kvp.Value.Name,
                Value = kvp.Value.Value,
                IdType = kvp.Value.IdType,
                Address = kvp.Value.Address,
                FormalParameters = kvp.Value.FormalParameters
            };
        }

        return new CompiledScript(instructions, clonedSymbols, Error);
    }

    public void SetExternalValue(string name, object? value)
    {
        if (externalSymbols.TryGetValue(name, out var identifier))
        {
            identifier.Value = ScriptValue.FromObject(value);
        }
        else
        {
            externalSymbols[name] = new Identifier { Name = name, Value = ScriptValue.FromObject(value), IdType = Identifier.IdentifierTypes.IdVariable };
        }
    }
}
