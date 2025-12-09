namespace Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;

public class MultiFactorApiResponse<TModel>
{
    public bool Success { get; set; }
    public TModel Model { get; set; }
}