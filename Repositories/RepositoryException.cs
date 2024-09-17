namespace Klacks_api.Repositories;

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
