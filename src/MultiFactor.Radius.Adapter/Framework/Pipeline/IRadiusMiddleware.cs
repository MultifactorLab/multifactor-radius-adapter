using MultiFactor.Radius.Adapter.Framework.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Framework.Pipeline;

/// <summary>
/// Radius pipeline middleware.
/// </summary>
public interface IRadiusMiddleware
{
    /// <summary>
    /// Executes middleware.
    /// </summary>
    /// <param name="context">Current request context.</param>
    /// <param name="next">Next middleware delegate.</param>
    /// <returns>Task.</returns>
    Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next);
}