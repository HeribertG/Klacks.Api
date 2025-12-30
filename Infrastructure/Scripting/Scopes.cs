using System.Diagnostics;

namespace Klacks.Api.Infrastructure.Scripting;

public class Scopes
{
    private readonly List<Scope> _scopes = new();

    public Identifier Allocate(string name, ScriptValue value = default, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.IdVariable)
    {
        return _scopes[^1].Allocate(name, value, idType);
    }

    public bool Assign(string name, ScriptValue value)
    {
        var variable = GetVariable(name);
        if (variable != null)
        {
            variable.Value = value;
            return true;
        }
        return false;
    }

    public bool Exists(string name, bool? inCurrentScopeOnly = false, Identifier.IdentifierTypes? idType = Identifier.IdentifierTypes.IdNone)
    {
        var n = inCurrentScopeOnly == true ? _scopes.Count - 1 : 0;

        for (var i = _scopes.Count - 1; i >= n; i--)
        {
            var s = _scopes[i];
            var renamed = s.GetVariable(name);

            if (renamed != null && renamed.Name == name)
            {
                if (idType == Identifier.IdentifierTypes.IdNone)
                    return true;

                if (idType == Identifier.IdentifierTypes.IdIsVariableOfFunction)
                    return renamed.IdType is Identifier.IdentifierTypes.IdVariable or Identifier.IdentifierTypes.IdFunction;

                if (idType == Identifier.IdentifierTypes.IdSubOfFunction)
                    return renamed.IdType is Identifier.IdentifierTypes.IdSub or Identifier.IdentifierTypes.IdFunction;

                if (idType == Identifier.IdentifierTypes.IdFunction)
                    return renamed.IdType is Identifier.IdentifierTypes.IdSub or Identifier.IdentifierTypes.IdFunction;

                if (idType == Identifier.IdentifierTypes.IdSub)
                    return renamed.IdType == Identifier.IdentifierTypes.IdSub;

                if (idType == Identifier.IdentifierTypes.IdVariable)
                    return renamed.IdType == Identifier.IdentifierTypes.IdVariable;
            }
        }

        return false;
    }

    public Identifier? Pop(int index = -1)
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
        {
            var s = _scopes[i];
            var x = s.Pop(index);
            if (x != null)
            {
                return x;
            }
        }
        return null;
    }

    public Identifier PopScopes(int? index = null)
    {
        var ind = index ?? -1;
        var scope = _scopes[^1];
        return scope.Pop(ind);
    }

    public void Push(ScriptValue value)
    {
        var s = _scopes[^1];
        s.Push(value);
    }

    public void PushScope(Scope? s = null)
    {
        _scopes.Add(s ?? new Scope());
    }

    public void PopScope()
    {
        if (_scopes.Count > 1)
        {
            _scopes.RemoveAt(_scopes.Count - 1);
        }
    }

    public Identifier? Retrieve(string name)
    {
        return GetVariable(name);
    }

    private Identifier? GetVariable(string name)
    {
        try
        {
            for (var i = _scopes.Count - 1; i >= 0; i--)
            {
                var s = _scopes[i];
                var x = s.GetVariable(name);
                if (x != null)
                {
                    return x;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Print("Scopes.GetVariable: " + ex.Message);
        }

        return null;
    }
}
