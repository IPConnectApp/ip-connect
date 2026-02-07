namespace ip_connect.Services.Files
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            // 1. Създаване на уникално име на файла
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // 2. Път към wwwroot/uploads/photos
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folderName);

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, fileName);

            // 3. Записване на диска
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. Връщане на релативен URL за браузъра
            return $"/uploads/{folderName}/{fileName}";
        }

        public void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            // Преобразува URL в локален път
            var relativePath = fileUrl.TrimStart('/');
            var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
    }
}
