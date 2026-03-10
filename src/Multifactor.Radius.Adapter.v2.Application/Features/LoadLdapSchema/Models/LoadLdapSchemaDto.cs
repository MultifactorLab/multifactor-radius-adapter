using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapSchema.Models
{
    public class LoadLdapSchemaDto
    {
        public string ConnectionString { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int BindTimeoutInSeconds { get; set; }
    }
}
