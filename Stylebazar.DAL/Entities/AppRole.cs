using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stylebazar.DAL.Entities
{
    // ROL entity-si — "AspNetRoles" cədvəli (istifadəçi rolları: Admin, Seller, Buyer).
    // IdentityRole<string>-dən miras alır → bütün lazımi sahələr (Id, Name və s.) hazır gəlir.
    // İçi boşdur, çünki əlavə sahəyə ehtiyac yoxdur — sadəcə tip adımızla istifadə edirik.
    public class AppRole : IdentityRole<string>
    {

    }
}
