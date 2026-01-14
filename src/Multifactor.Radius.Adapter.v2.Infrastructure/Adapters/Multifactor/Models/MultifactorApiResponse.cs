namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Models;

public class MultiFactorApiResponse<T>
{
    public bool Success { get; set; }
    public T Model { get; set; }
}