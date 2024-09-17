using System.Diagnostics;

namespace Klacks_api.BasicScriptInterpreter
{
  /// <summary>
  /// Das jeweils zuoberst liegende Scope-Objekt dient dabei als
  /// normaler Programm-Stack.
  ///
  /// Variablen symboltable (SyntaxAnalyser.cls) und scopes (Code.cls)
  /// sind Scopes-Objekte.
  /// </summary>
  public class Scopes
  {
    private List<Scope> _scopes = new List<Scope>();

    ~Scopes()
    {
      _scopes.Clear();
    }

    public Identifier Allocate(string name, object? value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.IdVariable)
    {
      return _scopes[_scopes.Count() - 1].Allocate(name, value, idType);
    }

    /// <summary>
    /// Von oben nach unten alle Scopes durchgehen und dem
    /// ersten benannten Wert mit dem übergebenen Namen den
    /// Wert zuweisen.
    /// </summary>
    public bool Assign(string name, object value)
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
      int i, n;
      Identifier renamed;
      Scope s;
      var result = false;

      n = inCurrentScopeOnly == true ? this._scopes.Count() - 1 : 0;

      for (i = _scopes.Count() - 1; i >= n; i += -1)
      {
        s = _scopes[i];
        renamed = (Identifier)s.GetVariable(name)!;

        if (renamed != null)
        {
          if (renamed.Name == name)
          {
            // Prüfen, ob gefundener Wert vom gewünschten Typ ist
            if (idType == Identifier.IdentifierTypes.IdNone)
            {
              result = true;
            }
            else
            {
              if (idType == Identifier.IdentifierTypes.IdIsVariableOfFunction)
              {
                if (renamed.IdType == Identifier.IdentifierTypes.IdVariable || renamed.IdType == Identifier.IdentifierTypes.IdFunction)
                {
                  result = true;
                }
              }
              else if (idType == Identifier.IdentifierTypes.IdSubOfFunction)
              {
                if (renamed.IdType == Identifier.IdentifierTypes.IdSub || renamed.IdType == Identifier.IdentifierTypes.IdFunction)
                {
                  result = true;
                }
              }
              else if (idType == Identifier.IdentifierTypes.IdFunction)
              {
                if (renamed.IdType == Identifier.IdentifierTypes.IdSub || renamed.IdType == Identifier.IdentifierTypes.IdFunction)
                {
                  result = true;
                }
              }
              else if (idType == Identifier.IdentifierTypes.IdFunction)
              {
                if (renamed.IdType == Identifier.IdentifierTypes.IdFunction)
                {
                  result = true;
                }
              }
              else if (idType == Identifier.IdentifierTypes.IdSub)
              {
                if (renamed.IdType == Identifier.IdentifierTypes.IdSub)
                {
                  result = true;
                }
              }
              else if (idType == Identifier.IdentifierTypes.IdVariable)
              {
                if (renamed.IdType == Identifier.IdentifierTypes.IdVariable)
                {
                  result = true;
                }
              }
            }
          }

          return result;
        }
      }

      return result;
    }

    public object? Pop(int index = -1)
    {
      Scope s;
      object x;
      for (int i = _scopes.Count - 1; i >= 0; i += -1)
      {
        s = _scopes[i];
        x = s.Pop(index);
        if (x != null)
        {
          return x;
        }
      }

      return null;
    }

    public Identifier PopScopes(int? index = null)
    {
      var ind = -1;
      if (index.HasValue)
      {
        ind = index.Value;
      }

      var scope = _scopes[_scopes.Count() - 1];
      var result = scope.Pop(ind);

      return result;
    }

    public void Push(object value)
    {
      if (value != null)
      {
        Scope s = _scopes[_scopes.Count() - 1];
        var c = new Identifier();
        c.Value = value;
        s.Push(c);
      }
    }

    public void PushScope(Scope? s = null)
    {
      if (s == null)
      {
        _scopes.Add(new Scope());
      }
      else
      {
        _scopes.Add(s);
      }
    }

    // dito, jedoch Wert zurückliefern (als kompletten Identifier)
    public Identifier? Retrieve(string name)
    {
      return GetVariable(name);
    }

    private Identifier? GetVariable(string name)
    {
      Scope s;
      Identifier x;
      try
      {
        for (int i = _scopes.Count - 1; i >= 0; i += -1)
        {
          s = _scopes[i];
          x = (Identifier)s.GetVariable(name)!;
          if (x != null)
          {
            return x;
          }
        }
      }
      catch (Exception ex)
      {
        Debug.Print("Scope.zVariable: " + ex.Message);
      }

      return null;
    }
  }
}
