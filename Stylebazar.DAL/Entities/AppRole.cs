// Microsoft.AspNetCore.Identity — IdentityRole<string> baza sinfini gətirir (hazır rol sistemi).
using Microsoft.AspNetCore.Identity;
// Aşağıdakı System ad fəzaları şablonla gəlib — saxlanılır.
using System;
using System.Collections.Generic;
using System.Text;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities
{
    // ROL entity-si — "AspNetRoles" cədvəli (rollar: Admin, Seller, Buyer).
    // IdentityRole<string>-dən miras alır → bütün lazımi sahələr (Id, Name, NormalizedName) hazır gəlir.
    // İçi BOŞDUR, çünki əlavə sahəyə ehtiyac yoxdur — sadəcə öz tip adımızla istifadə edirik.
    public class AppRole : IdentityRole<string>
    {

    }
}
