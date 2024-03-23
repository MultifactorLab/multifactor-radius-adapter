using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services
{
    public interface IRandomWaiter
    {
        Task WaitSomeTimeAsync();
    }
}