// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// ÜNVAN entity-si — "Addresses" cədvəli. Bir istifadəçinin çatdırılma/əlaqə ünvanları.
// Bir istifadəçinin bir neçə ünvanı ola bilər; yalnız BİRİ default (IsDefault) olur.
public class Address : Entity<int>
{
    // Ünvanın sahibi — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Qısa etiket: "Ev", "Ofis". Məcburidir.
    public required string Title { get; set; }
    // Alıcının ad-soyadı. Məcburidir.
    public required string FullName { get; set; }
    // Əlaqə telefonu. Məcburidir.
    public required string Phone { get; set; }
    // Şəhər. Məcburidir.
    public required string City { get; set; }
    // Rayon/bölgə. Opsionaldır.
    public string? District { get; set; }
    // Küçə + ev + mənzil (sərbəst mətn). Məcburidir.
    public required string AddressLine { get; set; }
    // Poçt indeksi. Opsionaldır.
    public string? PostalCode { get; set; }
    // Kuryer üçün qeyd/orientir. Opsionaldır.
    public string? Notes { get; set; }
    // Default ünvandırmı? İstifadəçidə yalnız biri true olur.
    public bool IsDefault { get; set; }

    // Ünvanın sahibi olan istifadəçi.
    public virtual AppUser User { get; set; } = null!;
}
