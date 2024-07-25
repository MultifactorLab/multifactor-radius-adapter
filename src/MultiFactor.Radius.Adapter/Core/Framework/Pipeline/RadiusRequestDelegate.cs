using MultiFactor.Radius.Adapter.Core.Framework.Context;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Framework.Pipeline;

/// <summary>
/// A function that can process an radius request.
/// </summary>
/// <param name="context">The <see cref="RadiusContext"/> for the request.</param>
/// <returns>A task that represents the completion of request processing.</returns>
public delegate Task RadiusRequestDelegate(RadiusContext context);
