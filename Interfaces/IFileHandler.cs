namespace Klacks.Api.Interfaces;

public interface IFileHandler
{
  void CreateDirectoryIfNotExists(string filePath);

  bool FileExists(string filePath);
}
