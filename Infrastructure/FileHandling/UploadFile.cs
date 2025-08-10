namespace Klacks.Api.Infrastructure.FileHandling
{
    public class UploadFile
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration configuration;
        private string? folderImage;
        private string? folderPdf;
        public UploadFile(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            configuration = config;
        }

        public void StoreFile(IFormFile file)
        {



            if (file != null)
            {

                if (IsImage(file))
                {
                    GetDocumentDirectoryImage();

                    using (var filestream = new FileStream(Path.Combine(folderImage!, file.FileName), FileMode.Create, FileAccess.Write))
                    {
                        file.CopyTo(filestream);
                    }
                }

                if (IsIcon(file))
                {
                    GetDocumentDirectoryImage();

                    using (var filestream = new FileStream(Path.Combine(folderImage!, file.FileName), FileMode.Create, FileAccess.Write))
                    {
                        file.CopyTo(filestream);
                    }
                }


                if (IsPDF(file))
                {

                    GetDocumentDirectoryPdf();
                    using (var filestream = new FileStream(Path.Combine(folderPdf!, file.FileName), FileMode.Create, FileAccess.Write))
                    {
                        file.CopyTo(filestream);
                    }
                }
            }


        }

        private bool IsPDF(IFormFile file)
        {
            //-------------------------------------------
            //  Check the image mime types
            //-------------------------------------------
            if (file.ContentType.ToLower() != "application/pdf")
            {
                return false;
            }

            //-------------------------------------------
            //  Check the image extension
            //-------------------------------------------
            if (Path.GetExtension(file.FileName)?.ToLower() != ".pdf")
            {
                return false;
            }

            return true;
        }

        private bool IsImage(IFormFile file)
        {

            if (file.ContentType.ToLower() != "image/jpg" &&
                file.ContentType.ToLower() != "image/jpeg" &&
                file.ContentType.ToLower() != "image/pjpeg" &&
                file.ContentType.ToLower() != "image/gif" &&
                file.ContentType.ToLower() != "image/x-png" &&
                file.ContentType.ToLower() != "image/png")
            {
                return false;
            }



            //-------------------------------------------
            //  Check the image extension
            //-------------------------------------------
            if (Path.GetExtension(file.FileName)?.ToLower() != ".jpg"
                && Path.GetExtension(file.FileName)?.ToLower() != ".png"
                && Path.GetExtension(file.FileName)?.ToLower() != ".gif"
                && Path.GetExtension(file.FileName)?.ToLower() != ".jpeg")
            {
                return false;
            }

            return true;
        }

        private bool IsIcon(IFormFile file)
        {

            if (file.ContentType.ToLower() != "image/x-icon")
            {
                return false;
            }



            //-------------------------------------------
            //  Check the image extension
            //-------------------------------------------
            if (Path.GetExtension(file.FileName)?.ToLower() != ".ico"
                && Path.GetExtension(file.FileName)?.ToLower() != ".png")
            {
                return false;
            }

            return true;
        }
        private void GetDocumentDirectoryImage()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Images"];
            var docuDirectory = Path.Combine(baseDirectory!, path);

            if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }

            folderImage = docuDirectory;
        }

        private void GetDocumentDirectoryPdf()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Documents"];
            var docuDirectory = Path.Combine(baseDirectory!, path);

            if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }

            folderPdf = docuDirectory;
        }


    }
}
