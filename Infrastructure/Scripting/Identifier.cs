namespace Klacks.Api.Infrastructure.Scripting;

public class Identifier
{
    public enum IdentifierTypes
    {
        IdIsVariableOfFunction = -2,
        IdSubOfFunction = -1,
        IdNone = 0,
        IdConst = 1,
        IdVariable = 2,
        IdFunction = 4,
        IdSub = 8,
    }

    public int Address { get; set; }
    public List<object>? FormalParameters { get; set; }
    public IdentifierTypes IdType { get; set; }
    public string? Name { get; set; }
    public ScriptValue Value { get; set; }
}
