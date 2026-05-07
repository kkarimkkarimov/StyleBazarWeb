namespace Stylebazar.BLL.Settings;

public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute filesystem path where uploaded files are saved.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>URL prefix that maps to RootPath (must match StaticFiles config).</summary>
    public string BaseUrl { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
}
