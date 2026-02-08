namespace Klacks.Api.Domain.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
