using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Athena.Models;

namespace Athena.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Athena.Models.Group> Group { get; set; }
        public DbSet<Athena.Models.Template> Template { get; set; }
        public DbSet<Athena.Models.Group_Template> Group_Template { get; set; }
        public DbSet<Athena.Models.Group_User> Group_User { get; set; }
        public DbSet<Athena.Models.Reservation> Reservation { get; set; }
        public DbSet<Athena.Models.Request> Request { get; set; }
    }
}
