namespace Klacks.Api.Infrastructure.Repositories;

public class RepositoryException : Exception
{
    public RepositoryException()
      : base()
    {
    }

    public RepositoryException(string message)
      : base(message)
    {
    }
}
