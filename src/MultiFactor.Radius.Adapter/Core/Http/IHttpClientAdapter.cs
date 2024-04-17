using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Http
{
    public interface IHttpClientAdapter
    {
        Task<T> PostAsync<T>(string endpoint, object data, IReadOnlyDictionary<string, string> headers = null);
    }
}