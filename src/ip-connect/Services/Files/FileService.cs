using ip_connect.Services.BlobStorage;

namespace ip_connect.Services.Files
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IBlobStorageService _blobStorageService;

        public FileService(IWebHostEnvironment environment, IBlobStorageService blobStorageService)
        {
            _environment = environment;
            _blobStorageService = blobStorageService;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            using (var stream = file.OpenReadStream())
            {
                return await _blobStorageService.UploadAlbumPhotoAsync(stream, file.FileName);
            }
        }

        public void DeleteFile(string fileUrl)
        {
            // REPLACE ENTIRE METHOD WITH THIS:
            if (string.IsNullOrEmpty(fileUrl)) return;

            // Fire and forget - don't wait for deletion
            _ = _blobStorageService.DeleteFileAsync(fileUrl);
        }
    }
}
