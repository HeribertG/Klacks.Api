namespace Klacks.Api.Infrastructure.Scripting;

public sealed record ScriptError(int Code, string Description, int Line, int Column);
