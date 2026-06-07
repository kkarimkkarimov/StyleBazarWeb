using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Subscriptions;

// AdminSubscriptionRowDto — admin qlobal abunəlik/ödəniş siyahısında BİR sətir.
// İstifadəçi məlumatı + plan + status + tarixlər + mənbə (Notes: Stripe ref və ya admin qeydi).
public class AdminSubscriptionRowDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }                  // "Stripe ödənişi · ref: cs_test_..." və ya admin qeydi
    public string? ApprovedByAdminFullName { get; set; }
    public DateTime CreatedDate { get; set; }
}
