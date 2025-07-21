namespace Klacks.Api.Exceptions;

/// <summary>
/// s thrown if a command is invalid.
/// </summary>
public class InvalidRequestException : Exception
{
    public InvalidRequestException(string message)
        : base(message)
    {
    }
}
