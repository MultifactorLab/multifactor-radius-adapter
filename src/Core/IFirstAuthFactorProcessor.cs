﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Server;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Core
{
    /// <summary>
    /// First authentication factor processor.
    /// </summary>
    public interface IFirstAuthFactorProcessor
    {
        /// <summary>
        /// Returns auth sources supported by the current processor implementation.
        /// </summary>
        AuthenticationSource AuthenticationSource { get; }

        /// <summary>
        /// Performs first authentication factor processing and returns <see cref="PacketCode"/> as a result.
        /// </summary>
        /// <param name="request">The pending request for which the authentication factor should be processed.</param>
        /// <param name="clientConfig">Current client configuration.</param>
        /// <returns><see cref="PacketCode"/> as a result of processing.</returns>
        Task<PacketCode> ProcessFirstAuthFactorAsync(PendingRequest request, ClientConfiguration clientConfig);
    }
}