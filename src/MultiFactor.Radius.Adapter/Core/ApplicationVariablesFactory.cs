using System;
using System.IO;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Core
{
    public static class ApplicationVariablesFactory
    {
        public static ApplicationVariables Create()
        {
            return new ApplicationVariables
            {
                AppPath = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}",
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                StartedAt = DateTime.Now
            };
        }
    }
}
