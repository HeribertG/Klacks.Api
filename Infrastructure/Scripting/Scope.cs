namespace Klacks.Api.Infrastructure.Scripting;

public class Scope
{
    private readonly List<Identifier> _variables = new();

    public Identifier Allocate(string name, ScriptValue value = default, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.IdVariable)
    {
        var id = new Identifier
        {
            Name = name,
            Value = value,
            IdType = idType
        };

        SetVariable(id, name);
        return id;
    }

    public void Assign(string name, ScriptValue value)
    {
        SetVariable(value, name);
    }

    public Identifier? Retrieve(string name)
    {
        return GetVariable(name);
    }

    public bool Exists(string name)
    {
        return GetVariable(name) != null;
    }

    public Identifier? GetVariable(string name)
    {
        return _variables.FirstOrDefault(x => x.Name == name);
    }

    public void SetVariable(ScriptValue value, string name)
    {
        var existing = _variables.FirstOrDefault(x => x.Name == name);
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        var c = new Identifier { Name = name, Value = value };
        if (_variables.Count == 0)
        {
            _variables.Add(c);
        }
        else
        {
            _variables.Insert(0, c);
        }
    }

    public void SetVariable(Identifier id, string name)
    {
        var existing = _variables.FirstOrDefault(x => x.Name == name);
        if (existing != null)
        {
            _variables.Remove(existing);
        }

        if (_variables.Count == 0)
        {
            _variables.Add(id);
        }
        else
        {
            _variables.Insert(0, id);
        }
    }

    public void Push(Identifier value)
    {
        _variables.Add(value);
    }

    public void Push(ScriptValue value)
    {
        _variables.Add(new Identifier { Value = value });
    }

    public Identifier Pop(int index = -1)
    {
        if (index < 0)
        {
            if (_variables.Count > 0)
            {
                var pop = _variables[^1];
                _variables.RemoveAt(_variables.Count - 1);
                return pop;
            }
            return new Identifier();
        }

        return _variables[^(1 + index)];
    }

    internal int CloneCount() => _variables.Count;

    public Identifier CloneItem(int index) => _variables[index];
}
