namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

//TODO ???
public sealed class ApplicationVariables
{
    public string? AppPath { get; init; }
    public string? AppVersion { get; init; }
    public DateTime StartedAt { get; init; }
    public TimeSpan UpTime => DateTime.Now - StartedAt;
}