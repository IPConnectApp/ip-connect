using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ip_connect.Services.BlobStorage
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _profilePicturesContainer = "profile-pictures";
        private readonly string _albumPhotosContainer = "album-photos";

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadProfilePictureAsync(Stream fileStream, string fileName)
        {
            return await UploadFileAsync(_profilePicturesContainer, fileStream, fileName);
        }

        public async Task<string> UploadAlbumPhotoAsync(Stream fileStream, string fileName)
        {
            return await UploadFileAsync(_albumPhotosContainer, fileStream, fileName);
        }

        private async Task<string> UploadFileAsync(string containerName, Stream fileStream, string fileName)
        {
            // Get container client (creates if doesn't exist)
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
         //   await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Generate unique filename to avoid conflicts
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload file
            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
            {
                ContentType = GetContentType(fileName)
            });

            // Return public URL
            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                // Extract blob name from URL
                var uri = new Uri(fileUrl);
                var segments = uri.Segments;
                var containerName = segments[1].TrimEnd('/');
                var blobName = string.Join("", segments.Skip(2));

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}