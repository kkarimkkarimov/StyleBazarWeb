// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — ContactType enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// ƏLAQƏ KLİKİ entity-si — "ContactClicks" cədvəli. Alıcının məhsul/mağaza üzərində hər əlaqə
// cəhdi (WhatsApp / Instagram / telefon klikləməsi) bir sətir kimi yazılır.
// Alıcı anonim ola bilər (UserId null). IP GDPR üçün anonimləşdirilir (son hissə sıfırlanır).
// Satıcı analitika panelini qidalandırır və premium abunəlik modelinin əsas metrikasıdır.
public class ContactClick : Entity<int>
{
    // Hansı məhsul (məhsul səhifəsindəki klikdə). null ola bilər (mağaza səhifəsindəki klik).
    public int? ProductId { get; set; }
    // Mağaza həmişə var — hər klik bir mağazaya bağlıdır (FK → Store).
    public int StoreId { get; set; }
    // Giriş etmiş alıcının id-si (FK → AppUser); anonim üçün null.
    public string? UserId { get; set; }
    // Kanal: WhatsApp / Instagram / telefon...
    public ContactType ContactType { get; set; }
    // Son okteti sıfırlanmış IP (məs. 192.168.1.0) — məxfilik üçün. Opsionaldır.
    public string? IpAddress { get; set; }
    // Cihaz/brauzer məlumatı (analitikada cihaz növü üçün). Opsionaldır.
    public string? UserAgent { get; set; }
    // Hansı səhifədən gəlib (HTTP Referer). Opsionaldır.
    public string? Referer { get; set; }

    // Aid olduğu məhsul (varsa).
    public virtual Product? Product { get; set; }
    // Aid olduğu mağaza.
    public virtual Store Store { get; set; } = null!;
    // Klikləyən istifadəçi (varsa).
    public virtual AppUser? User { get; set; }
}
