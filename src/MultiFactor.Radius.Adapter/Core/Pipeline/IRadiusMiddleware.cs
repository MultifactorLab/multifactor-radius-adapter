using MultiFactor.Radius.Adapter.Server.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public delegate Task RadiusRequestDelegate(RadiusContext context);

public interface IRadiusMiddleware
{
    Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next);
}