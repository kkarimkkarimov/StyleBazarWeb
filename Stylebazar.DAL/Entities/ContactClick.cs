using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// ƏLAQƏ KLİKİ entity-si — "ContactClicks" cədvəli. Alıcının məhsul/mağaza üzərində hər əlaqə
// cəhdi (WhatsApp / Instagram / telefon klikləməsi) bir sətir kimi yazılır.
// Alıcı anonim ola bilər (UserId null). IP GDPR üçün anonimləşdirilir (son hissə sıfırlanır).
//
// Satıcı analitika panelini qidalandırır və premium abunəlik modelinin əsas metrikasıdır
// (satıcı gəlir/dəyər görür, platforma listinqin dəyərini göstərir).
public class ContactClick : Entity<int>
{
    public int? ProductId { get; set; }            // Hansı məhsul (məhsul səhifəsində klik olduqda); null ola bilər

    public int StoreId { get; set; }               // Mağaza həmişə var — hər klik bir mağazaya bağlıdır

    public string? UserId { get; set; }            // Giriş etmiş alıcının id-si; anonim üçün null

    public ContactType ContactType { get; set; }   // Kanal: WhatsApp / Instagram / telefon...

    public string? IpAddress { get; set; }         // Son okteti sıfırlanmış IP (məs. 192.168.1.0)

    public string? UserAgent { get; set; }         // Cihaz/brauzer məlumatı (analitikada cihaz növü üçün)

    public string? Referer { get; set; }           // Hansı səhifədən gəlib (HTTP Referer); opsional

    public virtual Product? Product { get; set; }          // Aid olduğu məhsul (varsa)
    public virtual Store Store { get; set; } = null!;      // Aid olduğu mağaza
    public virtual AppUser? User { get; set; }             // Klikləyən istifadəçi (varsa)
}
