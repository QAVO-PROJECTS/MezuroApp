using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

public  class CloudinaryFileService : IFileService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFileService(IOptions<CloudinarySettings> options)
    {
        var cfg = options.Value;

        var account = new Account(
            cfg.CloudName,
            cfg.ApiKey,
            cfg.ApiSecret
        );

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadFile(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new GlobalAppException("FILE_REQUIRED");

        await using var stream = file.OpenReadStream();

        var ext = Path.GetExtension(file.FileName);
        var publicId = $"{folder}/{Guid.NewGuid():N}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = Guid.NewGuid().ToString("N"),
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result == null || result.StatusCode != System.Net.HttpStatusCode.OK)
            throw new GlobalAppException("FILE_UPLOAD_FAILED");

        return result.SecureUrl?.ToString()
               ?? throw new GlobalAppException("FILE_UPLOAD_FAILED");
    }
}