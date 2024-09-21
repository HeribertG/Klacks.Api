namespace Klacks.Api.Interfaces;

public interface IUnitOfWork
  {
      Task CompleteAsync();
      int Complete();
  }
