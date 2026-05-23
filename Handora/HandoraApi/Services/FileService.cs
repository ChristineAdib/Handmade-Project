using HandoraApplication.IServices;

namespace HandoraApi.Services;

public class FileService(IWebHostEnvironment environment) : IFileService
{
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", folder);

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/images/{folder}/{fileName}";
    }

    public void DeleteFile(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));

        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
