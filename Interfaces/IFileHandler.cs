namespace Klacks_api.Interfaces;

public interface IFileHandler
{
  void CreateDirectoryIfNotExists(string filePath);

  bool FileExists(string filePath);
}
