namespace Klacks.Api.Domain.Enums;

public enum MacroTypeEnum
{
    defaultResult = 1,
    info = 5,
    filter = 8000
}

public enum MacroFilterEnum
{
    clientType = 1,
    ownerDefinedValue = 2,
    bindOR = 3,
    bindAND = 4,
}
