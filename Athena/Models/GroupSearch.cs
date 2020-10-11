using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Athena.Models
{
    public class GroupSearch
    {
       public Group group { get; set; }
       public IEnumerable<Athena.Models.Group_User> group_user {get; set;}
       public IEnumerable<Athena.Models.Group_Template> group_template { get; set; }
    }
}
