namespace Klacks.Api.Infrastructure.Scripting;

public sealed class CompiledScript
{
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
        var errorObject = new InterpreterError();
        var sourceStream = new StringInputStream();
        var parser = new SyntaxAnalyser(errorObject);

        var code = new Code();
        parser.Parse(sourceStream.Connect(source), code, optionExplicit, allowExternal);

        ScriptError? error = null;
        if (errorObject.Number != 0)
        {
            error = new ScriptError(
                errorObject.Number,
                errorObject.Description,
                errorObject.Line,
                errorObject.Col
            );
        }

        var instructions = new List<object[]>();
        for (int i = 0; i < code.Count; i++)
        {
            instructions.Add((object[])code.GetInstruction(i));
        }

        var externalSymbols = new Dictionary<string, Identifier>();
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
