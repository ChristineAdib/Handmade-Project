// HandoraMVC/Services/NullFileService.cs
using HandoraApplication.IServices;

namespace HandoraMVC.Services;

public class NullFileService : IFileService
{
    public Task<string> UploadFileAsync(IFormFile file, string folder)
        => Task.FromResult(string.Empty);

    public Task<string> UploadRawFileAsync(IFormFile file, string folder)
        => Task.FromResult(string.Empty);

    public Task DeleteFileAsync(string fileUrl)
        => Task.CompletedTask;

    public Task DeleteFilesAsync(IEnumerable<string> fileUrls)
        => Task.CompletedTask;
}