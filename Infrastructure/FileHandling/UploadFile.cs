// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.FileHandling
{
    public class UploadFile : IFileUploadService
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
            if (file == null) return;

            var safeFileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName)) return;

            if (IsImage(file))
            {
                GetDocumentDirectoryImage();

                using (var filestream = new FileStream(Path.Combine(folderImage!, safeFileName), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(filestream);
                }
            }

            if (IsIcon(file))
            {
                GetDocumentDirectoryImage();

                using (var filestream = new FileStream(Path.Combine(folderImage!, safeFileName), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(filestream);
                }
            }

            if (IsPDF(file))
            {
                GetDocumentDirectoryPdf();
                using (var filestream = new FileStream(Path.Combine(folderPdf!, safeFileName), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(filestream);
                }
            }
        }

        private static bool IsPDF(IFormFile file)
        {
            return file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                && Path.GetExtension(file.FileName)?.Equals(".pdf", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsImage(IFormFile file)
        {
            var contentType = file.ContentType.ToLowerInvariant();
            var allowedTypes = new[] { "image/jpg", "image/jpeg", "image/pjpeg", "image/gif", "image/x-png", "image/png" };
            if (!allowedTypes.Contains(contentType)) return false;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            return ext is ".jpg" or ".png" or ".gif" or ".jpeg";
        }

        private static bool IsIcon(IFormFile file)
        {
            if (!file.ContentType.Equals("image/x-icon", StringComparison.OrdinalIgnoreCase)) return false;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            return ext is ".ico" or ".png";
        }
        private void GetDocumentDirectoryImage()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Images"];
            var docuDirectory = Path.Combine(baseDirectory!, path!);

            if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }

            folderImage = docuDirectory;
        }

        private void GetDocumentDirectoryPdf()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var path = configuration["CurrentPaths:Documents"];
            var docuDirectory = Path.Combine(baseDirectory!, path!);

            if (!Directory.Exists(docuDirectory)) { Directory.CreateDirectory(docuDirectory); }

            folderPdf = docuDirectory;
        }


    }
}
