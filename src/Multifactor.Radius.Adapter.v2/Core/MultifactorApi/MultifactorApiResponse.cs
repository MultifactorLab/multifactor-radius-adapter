namespace Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

public class MultiFactorApiResponse<TModel>
{
    public bool Success { get; set; }
    public TModel Model { get; set; }
}