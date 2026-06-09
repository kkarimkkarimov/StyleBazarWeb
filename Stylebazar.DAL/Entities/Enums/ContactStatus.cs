// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// ƏLAQƏ MESAJININ VƏZİYYƏTİ enum-u — admin "Bizimlə əlaqə" mesajını idarə edərkən işlədir.
public enum ContactStatus
{
    New = 0,         // Yeni (hələ baxılmayıb).
    InProgress = 1,  // Üzərində işlənir.
    Resolved = 2,    // Həll olunub.
    Closed = 3       // Bağlanıb.
}
