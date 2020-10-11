using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Athena.Models
{
    public partial class Request
    {
        [Display(Name = "Id")]
        public int RequestId { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        [Display(Name = "Created By")]
        public virtual IdentityUser CreatedByUser { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Text { get; set; }
    }
}


