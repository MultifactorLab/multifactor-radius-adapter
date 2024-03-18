﻿using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;

namespace MultiFactor.Radius.Adapter.Server
{
    internal static class AuthenticationStateExtensions
    {
        public static PacketCode ToPacketCode(this AuthenticationState authenticationState)
        {
            if ((authenticationState.FirstFactor == AuthenticationCode.Accept || authenticationState.FirstFactor == AuthenticationCode.Bypass)
                &&
                (authenticationState.SecondFactor == AuthenticationCode.Accept || authenticationState.SecondFactor == AuthenticationCode.Bypass))
            {
                return PacketCode.AccessAccept;
            }

            if (authenticationState.FirstFactor == AuthenticationCode.Reject || authenticationState.SecondFactor == AuthenticationCode.Reject)
            {
                return PacketCode.AccessReject;
            }

            return PacketCode.AccessChallenge;
        }
    }
}