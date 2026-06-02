using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Settings;

namespace Stylebazar.BLL.Services.FileStorage;

// LocalFileStorageService — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;

    // Magic-byte signatures so we don't trust the Content-Type header alone.
    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        ["image/jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        ["image/webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } // RIFF (then ....WEBP)
    };

    public LocalFileStorageService(IOptions<FileStorageSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken ct = default)
    {
        ValidateFile(file);

        string targetFolder = Path.Combine(_settings.RootPath, subfolder);
        Directory.CreateDirectory(targetFolder);

        string ext = GetSafeExtension(file);
        string fileName = $"{Guid.NewGuid():N}{ext}";
        string fullPath = Path.Combine(targetFolder, fileName);

        await using FileStream fs = File.Create(fullPath);
        await file.CopyToAsync(fs, ct);

        // URL with forward slashes regardless of OS.
        string url = $"{_settings.BaseUrl.TrimEnd('/')}/{subfolder}/{fileName}";
        return url;
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Task.CompletedTask;

        // Strip the BaseUrl prefix and resolve against RootPath.
        string baseUrl = _settings.BaseUrl.TrimEnd('/');
        if (!url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        string relative = url[baseUrl.Length..].TrimStart('/');
        // Prevent path traversal (..\, ..// etc.)
        if (relative.Contains("..", StringComparison.Ordinal))
            return Task.CompletedTask;

        string fullPath = Path.Combine(_settings.RootPath, relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new BusinessException("No file uploaded.");

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new BusinessException($"File exceeds {_settings.MaxFileSizeBytes / 1024 / 1024} MB limit.");

        string contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!_settings.AllowedContentTypes.Contains(contentType))
            throw new BusinessException($"Content type '{contentType}' is not allowed.");

        if (!HasValidSignature(file, contentType))
            throw new BusinessException("File contents do not match the declared image type.");
    }

    private static bool HasValidSignature(IFormFile file, string contentType)
    {
        if (!Signatures.TryGetValue(contentType, out byte[][]? signatures))
            return false;

        using Stream stream = file.OpenReadStream();
        Span<byte> head = stackalloc byte[16];
        int read = stream.Read(head);
        stream.Position = 0; // reset for the actual save

        foreach (byte[] sig in signatures)
        {
            if (read < sig.Length) continue;
            bool ok = true;
            for (int i = 0; i < sig.Length; i++)
            {
                if (head[i] != sig[i]) { ok = false; break; }
            }
            if (ok) return true;
        }
        return false;
    }

    private static string GetSafeExtension(IFormFile file) =>
        file.ContentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };
}
