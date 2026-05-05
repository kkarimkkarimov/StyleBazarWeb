<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Stylebazar.DAL.Entities
{
    internal class AppUser
    {
    }
=======
using Microsoft.AspNetCore.Identity;

namespace Stylebazar.DAL.Entities;

public class AppUser : IdentityUser<string>
{
    public required string FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
>>>>>>> claude/sharp-dijkstra-627d3d
}
