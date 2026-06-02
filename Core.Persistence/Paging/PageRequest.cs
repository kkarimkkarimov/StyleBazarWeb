namespace Core.Persistence.Paging;

// İstifadəçidən gələn səhifələmə istəyi — "neçənci səhifə, neçə element" soruşur.
// Adətən controller-ə parametr kimi gəlir (məs. ?pageIndex=0&pageSize=20).
public class PageRequest
{
    public int PageIndex { get; set; }  // İstənilən səhifənin nömrəsi (0-dan başlayır)
    public int PageSize { get; set; }   // Bir səhifədə neçə element istənilir
}
