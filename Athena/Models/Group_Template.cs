using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Athena.Models
{
   
        public partial class Group_Template
        {
            public int Id { get; set; }
        [Required]
        public int GroupId { get; set; }
        [Required]
        public int TemplateId { get; set; }

            public virtual Group Group { get; set; }
            public virtual Template Template { get; set; }
        }
 
}
