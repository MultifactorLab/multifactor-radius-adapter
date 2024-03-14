using MultiFactor.Radius.Adapter.Server.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public interface IRadiusPipeline
{
    Task InvokeAsync(RadiusContext context);
}
