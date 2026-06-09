// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// ABUNƏLİK PLANI enum-u — satıcılar üçün premium səviyyələr. Hər hesab üçün default Free-dir.
// Rəqəm dəyərlər bazada saxlanır, ona görə sıranı/adı dəyişmək data migration tələb edir.
public enum SubscriptionPlan
{
    Free = 0,   // Default. 5 məhsul limiti, analitika yox, təsdiq nişanı yox.
    Pro = 1,    // 50 məhsul limiti, tam analitika paneli, ~₼15/ay.
    VIP = 2     // 500 məhsul limiti, qabaqcıl analitika, təsdiq nişanı, kataloqda üstünlük, ~₼49/ay.
}
