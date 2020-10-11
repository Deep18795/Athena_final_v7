using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Athena.Models
{
    public class Group_User
    {
        public int Id { get; set; }
        [Required]
        public int GroupId { get; set; }
        [Required]
        public string UserId { get; set; }
        public virtual Group Group { get; set; }
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
    }
}
