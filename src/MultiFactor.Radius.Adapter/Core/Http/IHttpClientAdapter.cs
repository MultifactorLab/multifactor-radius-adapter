using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core.Http
{
    public interface IHttpClientAdapter
    {
        Task<T> DeleteAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null);
        Task<string> GetAsync(string uri, IReadOnlyDictionary<string, string> headers = null);
        Task<T> GetAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null);
        Task<T> PostAsync<T>(string uri, object data = null, IReadOnlyDictionary<string, string> headers = null);
    }
}