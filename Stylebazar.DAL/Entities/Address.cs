using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// A shipping / contact address belonging to a single user. Sellers use these
/// addresses for the "send package from" hint and buyers for the "deliver to"
/// metadata on outbound seller messages. A user can have multiple addresses;
/// exactly one is flagged <see cref="IsDefault"/>.
/// </summary>
public class Address : Entity<int>
{
    /// <summary>Owning user (FK to AppUser.Id).</summary>
    public required string UserId { get; set; }

    /// <summary>Short label — "Home", "Ofis", "Anamın evi". Max 60 chars.</summary>
    public required string Title { get; set; }

    /// <summary>Recipient full name. Max 100 chars.</summary>
    public required string FullName { get; set; }

    /// <summary>Recipient phone — store as user typed, validated at DTO layer.</summary>
    public required string Phone { get; set; }

    /// <summary>City. Required. Max 80 chars.</summary>
    public required string City { get; set; }

    /// <summary>Region / district. Optional. Max 80 chars.</summary>
    public string? District { get; set; }

    /// <summary>Street + house + flat (free-form). Required. Max 300 chars.</summary>
    public required string AddressLine { get; set; }

    /// <summary>Postal / ZIP code. Optional. Max 16 chars.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Landmark / courier note. Optional. Max 300 chars.</summary>
    public string? Notes { get; set; }

    /// <summary>True for the user's single default address.</summary>
    public bool IsDefault { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
