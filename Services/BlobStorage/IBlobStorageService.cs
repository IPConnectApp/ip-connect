namespace ip_connect.Services.BlobStorage
{
    public interface IBlobStorageService
    {
        Task<string> UploadProfilePictureAsync(Stream fileStream, string fileName);
        Task<string> UploadAlbumPhotoAsync(Stream fileStream, string fileName);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}