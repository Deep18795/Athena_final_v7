using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;


namespace Athena.Models
{
    public partial class Template
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Template()
        {
            this.Group_Template = new HashSet<Group_Template>();
        }
        [Required]
        public int TemplateId { get; set; }
        [Required]
        public string Path { get; set; }
        [Required (ErrorMessage = "The Template Name field is required.")]
        public string TemplateName { get; set; }
        [Required (ErrorMessage = "The Lab Label field is required.")]
        public string Lab { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Group_Template> Group_Template { get; set; }
    }

}
