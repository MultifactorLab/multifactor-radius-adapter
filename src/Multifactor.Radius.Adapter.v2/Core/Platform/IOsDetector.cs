using System.Runtime.InteropServices;

namespace Multifactor.Radius.Adapter.v2.Core.Platform;

public interface IOsDetector
{
    public OSPlatform GetCurrentOs();
}