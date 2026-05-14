using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// Shipping/billing address. Soft-deletable so historical orders keep their
/// snapshot intact even after the user deletes the address from their profile.
/// We also denormalise the address fields into Order so the order can be
/// rendered even if the address row is gone.
/// </summary>
public class Address : Entity<int>
{
    public required string UserId { get; set; }

    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string City { get; set; }
    public required string AddressLine { get; set; }

    /// <summary>Optional shorthand the user gives this address ("Ev", "Iş").</summary>
    public string? Label { get; set; }

    public string? PostalCode { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// At most one address per user can be the default. Checkout auto-selects
    /// it; the AddressManager enforces the invariant on Create/Update.
    /// </summary>
    public bool IsDefault { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
