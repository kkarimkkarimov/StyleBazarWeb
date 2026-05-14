namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// The lifecycle of a single order. Transitions:
///   Pending  →  Confirmed  →  Processing  →  Shipped  →  Delivered
///   any of the above  →  Cancelled (until Delivered)
///   Delivered  →  Returned  (within return window)
/// </summary>
public enum OrderStatus
{
    Pending = 0,        // just placed, awaiting confirmation
    Confirmed = 1,      // seller acknowledged
    Processing = 2,     // being packaged
    Shipped = 3,        // in transit
    Delivered = 4,      // received by buyer
    Cancelled = 5,      // cancelled by buyer or seller before delivery
    Returned = 6        // returned after delivery
}
