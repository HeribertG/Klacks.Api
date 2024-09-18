using Klacks_api.Helper;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Klacks_api.Controllers.V1.Backend;


public class LoadFileController : BaseController
{
    private readonly IConfiguration configuration;

    public LoadFileController(IConfiguration config)
    {
        configuration = config;
    }

    [HttpDelete("{type}")]
    public ActionResult DeleteFile(string type)
    {
        var path = GetFileFromDocumentDirectory(type);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
        return Ok();
    }

    //[HttpPost("Upload")]
    //[Consumes("multipart/form-data")]
    //public ActionResult SingleFile([FromForm] IFormFile file)
    //{
    //    if (file != null)
    //    {
    //        var sf = new UploadFile(configuration);

    //        sf.StoreFile(file);

    //        return Ok();
    //    }
    //    return Ok("No File");
    //}

    [HttpGet("DownLoad")]
    public async Task<FileContentResult> SingleFile(string type)
    {
        try
        {
            var task = await Task.Factory.StartNew((export) =>
            {
                var path = GetFileFromDocumentDirectory((string)export!);

                if (System.IO.File.Exists(path))
                {
                    byte[] result = System.IO.File.ReadAllBytes(path);
                    return File(result, "image/png");
                }
                else
                {
                    return File(Encoding.UTF8.GetBytes("File nicht gefunden"), "text/plain");
                }
            }, type);

            return task;
        }
        catch (Exception ex)
        {
            return File(Encoding.UTF8.GetBytes(ex.Message), "text/plain");
        }
    }

    private string GetFileFromDocumentDirectory(string type)
    {
        if (type.Contains("profile"))
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Images"];
            var docuDirectory = Path.Combine(baseDirectory, path);

            if (Directory.Exists(docuDirectory))
            {
                if (System.IO.File.Exists(Path.Combine(docuDirectory, type + ".png")))
                {
                    return Path.Combine(docuDirectory, type + ".png");
                }
                else if (System.IO.File.Exists(Path.Combine(docuDirectory, type + ".jpg")))
                {
                    return Path.Combine(docuDirectory, type + ".jpg");
                }
                else if (System.IO.File.Exists(Path.Combine(docuDirectory, type + ".jpeg")))
                {
                    return Path.Combine(docuDirectory, type + ".jpeg");
                }
                else if (System.IO.File.Exists(Path.Combine(docuDirectory, type + ".gif")))
                {
                    return Path.Combine(docuDirectory, type + ".gif");
                }
            }
        }
        else if (type == "own-icon.ico")
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Images"];
            var docuDirectory = Path.Combine(baseDirectory, path);

            if (Directory.Exists(docuDirectory))
            {
                return Path.Combine(docuDirectory, "own-icon.ico");
            }
        }
        else if (type == "own-logo.png")
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Images"];
            var docuDirectory = Path.Combine(baseDirectory, path);

            if (Directory.Exists(docuDirectory))
            {
                return Path.Combine(docuDirectory, "own-logo.png");
            }
        }
        else
        {
        }

        return null!;
    }
}
