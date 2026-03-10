using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.LoadProfile
{
    internal static class Module
    {
        public static IServiceCollection AddProfileSearch(this IServiceCollection services)
        {
            return services.AddTransient<IProfileSearch, LdapProfileSearch>();
        }
    }
}
