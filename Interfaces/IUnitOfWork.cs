namespace Klacks_api.Interfaces;

public interface IUnitOfWork
  {
      Task CompleteAsync();
      int Complete();
  }
