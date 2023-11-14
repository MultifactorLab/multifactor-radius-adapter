//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection.Exceptions
{
    [Serializable]
    internal class TechnicalAccountErrorException : Exception
    {
        public TechnicalAccountErrorException(string user, string domain)
            : base($"Unable to bind technical user '{user}' at domain '{domain}'") { }

        public TechnicalAccountErrorException(string user, string domain, Exception inner)
            : base($"Unable to bind technical user '{user}' at domain '{domain}': {inner.Message}", inner) { }

        protected TechnicalAccountErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}