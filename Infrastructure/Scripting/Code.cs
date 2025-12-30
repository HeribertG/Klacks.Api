namespace Klacks.Api.Infrastructure.Scripting;

public class Code : IDisposable
{
    private List<object> code = new();
    private Scope external = new();

    public int Count => code.Count;
    public InterpreterError? ErrorObject { get; private set; } = new();
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
}
