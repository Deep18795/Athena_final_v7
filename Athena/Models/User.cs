using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Athena.Models
{
    public class User : IdentityUser
    {
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public string UserName { get; set; }
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public string Email { get; set; }
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public int Id { get; set; }
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        public string Role { get; set; }

    }
}