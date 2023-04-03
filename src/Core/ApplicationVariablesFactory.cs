using System;
using System.IO;

namespace MultiFactor.Radius.Adapter.Core
{
    public static class ApplicationVariablesFactory
    {
        public static ApplicationVariables Create()
        {
            return new ApplicationVariables
            {
                AppPath = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}"
            };
        }
    }
}
