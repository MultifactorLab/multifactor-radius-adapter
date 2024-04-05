using System;

namespace MultiFactor.Radius.Adapter.Core
{
    public class ApplicationVariables
    {
        public string AppPath { get; init; }
        public string AppVersion { get; init; }
        public DateTime StartedAt { get; init; }
        public TimeSpan UpTime => DateTime.Now - StartedAt;
    }
}
