using Microsoft.AspNetCore.Http;

namespace Stylebazar.BLL.Services.FileStorage;

public interface IFileStorageService
{
    /// <summary>
    /// Saves an uploaded file under the given subfolder and returns the public URL.
    /// </summary>
    /// <param name="file">The uploaded file (multipart/form-data).</param>
    /// <param name="subfolder">e.g. "products", "stores", "users". Created if missing.</param>
    Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken ct = default);

    /// <summary>
    /// Removes a previously stored file by its public URL.
    /// Silently succeeds if the file is missing.
    /// </summary>
    Task DeleteAsync(string url, CancellationToken ct = default);
}
