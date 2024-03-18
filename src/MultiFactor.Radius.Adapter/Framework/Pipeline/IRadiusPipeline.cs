using MultiFactor.Radius.Adapter.Framework.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Framework.Pipeline;

/// <summary>
/// Pipeline which is invokes when processing each radius request.
/// </summary>
public interface IRadiusPipeline
{
    /// <summary>
    /// Envoke radius pipeline for the specified radius context.
    /// </summary>
    /// <param name="context">Radius context.</param>
    /// <returns>Task</returns>
    Task InvokeAsync(RadiusContext context);
}
