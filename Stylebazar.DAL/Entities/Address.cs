using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// ÜNVAN entity-si — "Addresses" cədvəli. Bir istifadəçinin çatdırılma/əlaqə ünvanları.
// Bir istifadəçinin bir neçə ünvanı ola bilər; yalnız BİRİ default (IsDefault) olur.
public class Address : Entity<int>
{
    public required string UserId { get; set; }        // Ünvanın sahibi (FK → AppUser)

    public required string Title { get; set; }          // Qısa etiket: "Ev", "Ofis"

    public required string FullName { get; set; }       // Alıcının ad-soyadı

    public required string Phone { get; set; }          // Əlaqə telefonu

    public required string City { get; set; }           // Şəhər (məcburi)

    public string? District { get; set; }               // Rayon/bölgə (opsional)

    public required string AddressLine { get; set; }    // Küçə + ev + mənzil (sərbəst mətn)

    public string? PostalCode { get; set; }             // Poçt indeksi (opsional)

    public string? Notes { get; set; }                  // Kuryer üçün qeyd/orientir (opsional)

    public bool IsDefault { get; set; }                 // Default ünvandır? (istifadəçidə yalnız biri)

    public virtual AppUser User { get; set; } = null!;  // Ünvanın sahibi
}
