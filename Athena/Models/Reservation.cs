using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Athena.Models
{
    public partial class Reservation
    {
        [Display( Name = "Id")]
        public int ReservationId { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedByUserId { get; set; }

        [Display(Name = "Start on")]
        [Required]
        public DateTime StartOn { get; set; }

        [Display(Name = "End on")]
        [Required]
        public DateTime EndOn { get; set; }

        [Display(Name = "Group")]
        [Required]
        public int GroupId { get; set; }

        [Display(Name = "Template")]
        [Required]
        public int TemplateId { get; set; }

        [ForeignKey("CreatedByUserId")]
        [Display(Name = "Created By")]
        public virtual IdentityUser CreatedByUser { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; }

        [ForeignKey("TemplateId")]
        public virtual Template Template { get; set; }

    }
}
