namespace Stylebazar.BLL.Settings;

// FAYL SAXLAMA AYARLARI — yüklənən şəkillərin (məhsul, profil və s.) saxlanma qaydaları.
public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    // Faylların yazıldığı tam disk yolu (məs. .../wwwroot/uploads).
    public string RootPath { get; set; } = string.Empty;

    // RootPath-a uyğun gələn URL prefiksi (StaticFiles konfiqi ilə eyni olmalıdır).
    public string BaseUrl { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;   // Maksimum fayl ölçüsü (10 MB)

    // İcazə verilən fayl tipləri (yalnız bu şəkil formatları qəbul olunur).
    public string[] AllowedContentTypes { get; set; } =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
}
