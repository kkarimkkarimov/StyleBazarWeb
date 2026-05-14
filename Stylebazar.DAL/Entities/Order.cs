using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// A placed order. Address fields are SNAPSHOTTED at order-time so the
/// historical record never changes even if the source Address row is edited
/// or deleted. Same for OrderItem (product snapshot).
/// </summary>
public class Order : Entity<int>
{
    public required string UserId { get; set; }

    /// <summary>
    /// Human-readable order number ("SB-2026-00042") for support / receipts.
    /// Auto-generated on Place.
    /// </summary>
    public required string OrderNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

    // --- shipping address snapshot ---
    public required string ShipFullName { get; set; }
    public required string ShipPhone { get; set; }
    public required string ShipCity { get; set; }
    public required string ShipAddressLine { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipNotes { get; set; }

    // --- money ---
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }

    /// <summary>Free-text buyer note (e.g. "after 18:00 please").</summary>
    public string? CustomerNote { get; set; }

    /// <summary>When the status was last changed (for SLA / dashboard).</summary>
    public DateTime? StatusChangedDate { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
