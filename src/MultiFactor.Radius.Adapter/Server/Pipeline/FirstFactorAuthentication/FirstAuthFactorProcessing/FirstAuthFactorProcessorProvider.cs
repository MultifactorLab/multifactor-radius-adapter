using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.FirstAuthFactorProcessing
{
    public class FirstAuthFactorProcessorProvider : IFirstAuthFactorProcessorProvider
    {
        private readonly IEnumerable<IFirstAuthFactorProcessor> _processors;

        public FirstAuthFactorProcessorProvider(IEnumerable<IFirstAuthFactorProcessor> processors)
        {
            _processors = processors ?? throw new ArgumentNullException(nameof(processors));
        }

        /// <summary>
        /// Returns implementation of <see cref="IFirstAuthFactorProcessor"/> for the specified authentication source.
        /// </summary>
        /// <param name="authSource">Authentication source.</param>
        /// <exception cref="NotImplementedException"></exception>
        public IFirstAuthFactorProcessor GetProcessor(AuthenticationSource authSource)
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
