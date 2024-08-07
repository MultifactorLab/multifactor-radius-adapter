﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.ActiveDirectory.MembershipVerification
{
    public class MembershipProcessingResultHandler
    {
        private readonly MembershipProcessingResult _processingResult;

        public MembershipProcessingResultHandler(MembershipProcessingResult processingResult)
        {
            _processingResult = processingResult ?? throw new ArgumentNullException(nameof(processingResult));
        }

        /// <summary>
        /// Returns Accept or Reject code as an overall result of multiple verification results.
        /// </summary>
        /// <returns><see cref="PacketCode.AccessAccept"/> or <see cref="PacketCode.AccessReject"/></returns>
        public PacketCode GetDecision()
        {
            return _processingResult.Succeeded.Any()
                ? PacketCode.AccessAccept
                : PacketCode.AccessReject;
        }

        /// <summary>
        /// Sets some request's property values.
        /// </summary>
        /// <param name="context">Pending request.</param>
        public void EnrichContext(RadiusContext context)
        {
            var profile = _processingResult.Succeeded.Select(x => x.Profile).FirstOrDefault(x => x != null);
            if (profile == null) 
            { 
                return; 
            }

            context.UpdateProfile(profile);

            var bypassed = IsBypassed();
            if (bypassed)
            {
                context.SetSecondFactorAuth(AuthenticationCode.Bypass);
            }
        }

        private bool IsBypassed()
        {
            var succeeded = _processingResult.Succeeded.ToList();
            if (!succeeded.Any())
            {
                return false;
            }

            if (succeeded.Any(x => x.IsMemberOf2FaBypassGroup))
            {
                return true;
            }

            if (succeeded.Any(x => x.Are2FaGroupsSpecified && !x.IsMemberOf2FaGroups))
            {
                return true;
            }

            return false;
        }
    }
}