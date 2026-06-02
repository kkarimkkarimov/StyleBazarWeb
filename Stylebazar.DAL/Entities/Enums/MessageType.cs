namespace Stylebazar.DAL.Entities.Enums;

// MESAJ NÖVÜ enum-u — mesaj nə daşıyır? Hər sətir dəqiq birini seçir:
//   - Text:         sərbəst mətn (default)
//   - Image:        AttachmentUrl yüklənmiş şəklə işarə edir
//   - StoryReply:   mətn + StoryId cavab verilən hekayəyə istinad (qabarcıq üstündə kiçik önizləmə)
//   - ProductShare: mətn + ProductId paylaşılan məhsul kartına istinad
public enum MessageType
{
    Text = 0,          // Mətn
    Image = 1,         // Şəkil
    StoryReply = 2,    // Hekayəyə cavab
    ProductShare = 3   // Məhsul paylaşımı
}
