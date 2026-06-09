// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// MESAJ NÖVÜ enum-u — mesaj nə daşıyır? Hər sətir dəqiq birini seçir.
public enum MessageType
{
    Text = 0,          // Sərbəst mətn (default).
    Image = 1,         // Şəkil — AttachmentUrl yüklənmiş şəklə işarə edir.
    StoryReply = 2,    // Hekayəyə cavab — mətn + StoryId cavab verilən hekayəyə istinad.
    ProductShare = 3   // Məhsul paylaşımı — mətn + ProductId paylaşılan məhsul kartına istinad.
}
