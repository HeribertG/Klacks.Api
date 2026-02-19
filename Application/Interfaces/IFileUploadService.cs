namespace Klacks.Api.Application.Interfaces;

public interface IFileUploadService
{
    void StoreFile(IFormFile file);
}
