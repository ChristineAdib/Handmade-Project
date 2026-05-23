using Microsoft.AspNetCore.Http;

namespace HandoraApplication.IServices;

public interface IFileService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    void DeleteFile(string fileUrl);
}
