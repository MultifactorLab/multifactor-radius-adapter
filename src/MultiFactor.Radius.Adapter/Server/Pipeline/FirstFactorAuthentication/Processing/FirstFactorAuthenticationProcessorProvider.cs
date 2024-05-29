using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing
{
    public class FirstFactorAuthenticationProcessorProvider : IFirstFactorAuthenticationProcessorProvider
    {
        private readonly IEnumerable<IFirstFactorAuthenticationProcessor> _processors;

        public FirstFactorAuthenticationProcessorProvider(IEnumerable<IFirstFactorAuthenticationProcessor> processors)
        {
            _processors = processors ?? throw new ArgumentNullException(nameof(processors));
        }

        /// <summary>
        /// Returns implementation of <see cref="IFirstFactorAuthenticationProcessor"/> for the specified authentication source.
        /// </summary>
        /// <param name="authSource">Authentication source.</param>
        /// <exception cref="NotImplementedException"></exception>
        public IFirstFactorAuthenticationProcessor GetProcessor(AuthenticationSource authSource)
        {
            if (authSource == AuthenticationSource.None)
            {
                return _processors.FirstOrDefault(x => x.AuthenticationSource == AuthenticationSource.None)
                    ?? throw new NotImplementedException($"Unexpected authentication source '{authSource}'.");
            }

            return _processors
                .FirstOrDefault(x => x.AuthenticationSource.HasFlag(authSource))
                    ?? throw new NotImplementedException($"Unexpected authentication source '{authSource}'.");
        }
    }
}
