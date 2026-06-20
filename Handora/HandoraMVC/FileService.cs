using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HandoraApplication.IServices;
using HandoraApplication.Settings;
using Microsoft.Extensions.Options;

namespace HandoraMVC.Services;

public class FileService : IFileService
{
    private readonly Cloudinary _cloudinary;

    public FileService(IOptions<CloudinarySettings> settings)
    {
        var account = new Account(
            settings.Value.CloudName,
            settings.Value.ApiKey,
            settings.Value.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        return result.SecureUrl.AbsoluteUri;
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        var publicId = ExtractPublicId(fileUrl);
        if (publicId == null)
            return;

        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public async Task DeleteFilesAsync(IEnumerable<string> fileUrls)
    {
        foreach (var url in fileUrls)
            await DeleteFileAsync(url);
    }

    private static string? ExtractPublicId(string url)
    {
        // Cloudinary URL format: https://res.cloudinary.com/cloudname/image/upload/v12345/folder/publicid.ext
        var match = Regex.Match(url, @"/upload/(?:v\d+/)?(.+?)(?:\.[a-z]+)?$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}
