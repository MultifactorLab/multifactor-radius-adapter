using System.Runtime.InteropServices;
using Multifactor.Radius.Adapter.v2.Core.Platform;

namespace Multifactor.Radius.Adapter.v2.Tests.OsDetectorTests;

public class OsDetectorTests
{
    [Fact]
    public void GetCurrentOsEnvironment_ShouldReturnOs()
    {
        var detector = new OsDetector();
        var environment = detector.GetCurrentOs();
        
        Assert.True(RuntimeInformation.IsOSPlatform(environment));
    }
}