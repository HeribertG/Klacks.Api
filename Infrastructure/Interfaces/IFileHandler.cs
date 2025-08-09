namespace Klacks.Api.Infrastructure.Interfaces;

public interface IFileHandler
{
    void CreateDirectoryIfNotExists(string filePath);

    bool FileExists(string filePath);
}
