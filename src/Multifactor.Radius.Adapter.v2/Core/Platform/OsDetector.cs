using System.Runtime.InteropServices;

namespace Multifactor.Radius.Adapter.v2.Core.Platform;

public class OsDetector : IOsDetector
{
    private readonly OSPlatform[] _platforms = new [] { OSPlatform.Linux, OSPlatform.Windows, OSPlatform.OSX, OSPlatform.FreeBSD };
    public OSPlatform GetCurrentOs()
    {
        foreach (var platform in _platforms)
        {
            if (RuntimeInformation.IsOSPlatform(platform))
                return platform;
        }

        throw new PlatformNotSupportedException();
    }
}