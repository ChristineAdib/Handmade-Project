using Microsoft.AspNetCore.Http;

namespace HandoraApplication.IServices;

public interface IFileService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    Task DeleteFileAsync(string fileUrl);
    Task DeleteFilesAsync(IEnumerable<string> fileUrls);
}
