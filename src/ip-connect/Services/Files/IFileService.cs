namespace ip_connect.Services.Files
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        void DeleteFile(string fileUrl);
    }
}
