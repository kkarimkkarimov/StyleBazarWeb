namespace Stylebazar.BLL.Settings;

// JWT (token) AYARLARI — appsettings-dəki "Jwt" bölməsindən oxunur.
// Token-lərin imzalanması və yoxlanması üçün lazımdır.
public class JwtSettings
{
    public const string SectionName = "Jwt";   // appsettings-dəki bölmənin adı

    public string Issuer { get; set; } = string.Empty;     // Token-i kim verir (emitent)
    public string Audience { get; set; } = string.Empty;   // Token kimin üçündür (auditoriya)
    public string Key { get; set; } = string.Empty;        // İmza açarı (gizli, ≥32 simvol)
    public int ExpireMinutes { get; set; } = 60;           // Token neçə dəqiqə etibarlıdır
}
