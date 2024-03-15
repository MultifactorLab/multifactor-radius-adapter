using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Http
{
    public interface IHttpClientAdapter
    {
        Task<T> PostAsync<T>(string uri, object data = null, IReadOnlyDictionary<string, string> headers = null);
    }
}